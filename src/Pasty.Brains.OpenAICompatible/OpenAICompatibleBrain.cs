using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Pasty.Core;
using Pasty.Core.Brains;

namespace Pasty.Brains.OpenAICompatible;

/// <summary>
/// One brain for every provider that speaks the OpenAI-style <c>/chat/completions</c> API:
/// GitHub Models, OpenAI, Gemini's compatibility endpoint, Ollama, LM Studio, Azure, OpenRouter…
/// Plain HttpClient and JSON, no SDK: the shape is small and stable across all of them.
/// </summary>
public sealed class OpenAICompatibleBrain : IPastyBrain, IDisposable
{
    private const int MaxTurnsBeforeReset = 40;

    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly string _model;
    private readonly string _systemPrompt;
    private readonly bool _isLocal;
    private readonly List<JsonObject> _history = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <param name="systemPrompt">Personality prompt for the selected character; defaults to Pasty.</param>
    public OpenAICompatibleBrain(string name, string endpoint, string model, string? apiKey, HttpMessageHandler? handler = null, string? systemPrompt = null)
    {
        _systemPrompt = systemPrompt ?? PastyPrompt.System;
        Name = name;
        _model = model;
        _endpoint = new Uri(endpoint.TrimEnd('/') + "/");
        _isLocal = _endpoint.IsLoopback;
        _http = CreateClient(_endpoint, apiKey, handler,
            // Local models on modest hardware can be slow to produce the first token.
            _isLocal ? TimeSpan.FromSeconds(180) : TimeSpan.FromSeconds(60));
    }

    public string Name { get; }

    public string? Model => _model;

    public bool IsRemote => !_isLocal;

    public void ResetConversation()
    {
        _gate.Wait();
        try { _history.Clear(); }
        finally { _gate.Release(); }
    }

    public async Task<Utterance> ReplyAsync(string userText, BrainContext context, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_history.Count >= MaxTurnsBeforeReset * 2)
                _history.Clear();

            var user = Message("user", PastyPrompt.WithContext(userText, context));
            var messages = new JsonArray { Message("system", _systemPrompt) };
            foreach (var m in _history) messages.Add(m.DeepClone());
            messages.Add(user.DeepClone());

            var body = new JsonObject { ["model"] = _model, ["messages"] = messages };
            using var response = await SendAsync(() => _http.PostAsJsonAsync("chat/completions", body, cancellationToken), cancellationToken);
            var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken)
                ?? throw new BrainException($"My {Name} brain sent back something I couldn't read.");

            var choice = json["choices"]?[0];
            var text = ContentText(choice?["message"]?["content"]);
            if (choice?["finish_reason"]?.GetValue<string>() == "content_filter")
                return new Utterance("I'd rather not help with that one. Ask me something else?", PastyReaction.Annoyed);
            if (string.IsNullOrWhiteSpace(text))
                throw new BrainException($"My {Name} brain went quiet. Try again, or pick a different model in Settings.");

            _history.Add(user);
            _history.Add(Message("assistant", text));
            return PastyPrompt.ParseMood(text);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Local servers like Ollama load the model on first use, which can take a minute. Sending a
    /// one-token request in the background means the first real question is answered promptly.
    /// Best effort: failures are ignored and surface normally on the first real request.
    /// </summary>
    public async Task WarmUpAsync()
    {
        if (!_isLocal) return;
        try
        {
            var body = new JsonObject
            {
                ["model"] = _model,
                ["messages"] = new JsonArray { Message("user", "hi") },
                ["max_tokens"] = 1,
            };
            using var response = await _http.PostAsJsonAsync("chat/completions", body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ObjectDisposedException)
        {
        }
    }

    /// <summary>Asks the provider which models it offers. GitHub Models keeps its list in a separate catalog.</summary>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var isGitHub = _endpoint.Host.Equals("models.github.ai", StringComparison.OrdinalIgnoreCase);
        var url = isGitHub ? new Uri(_endpoint, "/catalog/models") : new Uri(_endpoint, "models");
        using var response = await SendAsync(() => _http.GetAsync(url, cancellationToken), cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);

        // OpenAI style: { "data": [ { "id": ... } ] }. GitHub catalog: [ { "id": ... } ].
        var items = json is JsonArray array ? array : json?["data"] as JsonArray;
        return items?
            .Select(item => item?["id"]?.GetValue<string>())
            .OfType<string>()
            .Select(id => id.StartsWith("models/") ? id["models/".Length..] : id) // Gemini prefixes ids
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException && _isLocal)
        {
            throw new BrainException($"I can't reach {_endpoint.Authority}. Is your local model server (e.g. Ollama) running?", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new BrainException($"I can't reach my {Name} brain. Are we online?", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrainException($"My {Name} brain took too long to answer. Try again?", ex);
        }

        if (response.IsSuccessStatusCode) return response;

        var detail = await ErrorDetailAsync(response);
        response.Dispose();
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new BrainException($"My {Name} brain rejected the API key. Check Settings → Brain.{detail}"),
            HttpStatusCode.NotFound =>
                new BrainException($"My {Name} brain doesn't know the model '{_model}' (or the endpoint is wrong). Try 'List models' in Settings.{detail}"),
            HttpStatusCode.TooManyRequests =>
                new BrainException($"My {Name} brain is rate-limited. Give me a minute to catch my breath.{detail}"),
            _ => new BrainException($"My {Name} brain had a problem ({(int)response.StatusCode}).{detail}"),
        };
    }

    private static async Task<string> ErrorDetailAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            var message = JsonNode.Parse(text)?["error"] switch
            {
                JsonObject error => error["message"]?.GetValue<string>(),
                JsonValue value => value.GetValue<string>(),
                _ => null,
            };
            return string.IsNullOrWhiteSpace(message) ? "" : $" ({Truncate(message, 160)})";
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or HttpRequestException)
        {
            return "";
        }
    }

    /// <summary>Content is usually a string, but some providers return a list of typed parts.</summary>
    private static string? ContentText(JsonNode? content) => content switch
    {
        JsonValue value => value.GetValue<string>(),
        JsonArray parts => string.Concat(parts.Select(p => p?["text"]?.GetValue<string>())),
        _ => null,
    };

    private static JsonObject Message(string role, string content) => new() { ["role"] = role, ["content"] = content };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static HttpClient CreateClient(Uri endpoint, string? apiKey, HttpMessageHandler? handler, TimeSpan timeout)
    {
        var http = handler is null ? new HttpClient() : new HttpClient(handler);
        http.BaseAddress = endpoint;
        http.Timeout = timeout;
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Pasty/1.1 (+desktop companion)");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            // Azure OpenAI's key header; harmless elsewhere.
            if (endpoint.Host.EndsWith(".azure.com", StringComparison.OrdinalIgnoreCase))
                http.DefaultRequestHeaders.Add("api-key", apiKey);
        }
        if (endpoint.Host.Equals("models.github.ai", StringComparison.OrdinalIgnoreCase))
        {
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }
        return http;
    }

    public void Dispose()
    {
        _http.Dispose();
        _gate.Dispose();
    }
}
