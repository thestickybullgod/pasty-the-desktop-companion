using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Pasty.Brains.OpenAICompatible;
using Pasty.Core.Brains;
using Pasty.Core.Settings;

namespace Pasty.Core.Tests;

public class OpenAICompatibleBrainTests
{
    /// <summary>Records requests and answers with a canned response.</summary>
    private sealed class FakeServer(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, content));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static readonly BrainContext Now = new(new DateTime(2026, 10, 31, 21, 0, 0));

    private static string Completion(string text) =>
        new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = text } }) }.ToJsonString();

    [Fact]
    public async Task Sends_pasty_persona_and_parses_mood()
    {
        var server = new FakeServer(HttpStatusCode.OK, Completion("[smug] Obviously."));
        var brain = new OpenAICompatibleBrain("ChatGPT", "https://api.example.com/v1", "gpt-test", "sk-123", server);

        var reply = await brain.ReplyAsync("is paste a liquid?", Now, CancellationToken.None);

        Assert.Equal("Obviously.", reply.Text);
        Assert.Equal(PastyReaction.Smug, reply.Reaction);
        var (request, body) = server.Requests.Single();
        Assert.Equal("https://api.example.com/v1/chat/completions", request.RequestUri!.ToString());
        Assert.Equal("Bearer sk-123", request.Headers.Authorization!.ToString());
        var json = JsonNode.Parse(body)!;
        Assert.Equal("gpt-test", json["model"]!.GetValue<string>());
        Assert.Equal("system", json["messages"]![0]!["role"]!.GetValue<string>());
        Assert.Contains("Pasty", json["messages"]![0]!["content"]!.GetValue<string>());
    }

    [Fact]
    public async Task Keeps_conversation_history()
    {
        var server = new FakeServer(HttpStatusCode.OK, Completion("[happy] Hi!"));
        var brain = new OpenAICompatibleBrain("Ollama", "http://localhost:11434/v1", "llama3.2", null, server);

        await brain.ReplyAsync("first", Now, CancellationToken.None);
        await brain.ReplyAsync("second", Now, CancellationToken.None);

        var messages = JsonNode.Parse(server.Requests[1].Body)!["messages"]!.AsArray();
        Assert.Equal(["system", "user", "assistant", "user"], messages.Select(m => m!["role"]!.GetValue<string>()));
        Assert.Null(server.Requests[0].Request.Headers.Authorization);
        Assert.False(brain.IsRemote);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "rejected the API key")]
    [InlineData(HttpStatusCode.NotFound, "doesn't know the model")]
    [InlineData(HttpStatusCode.TooManyRequests, "rate-limited")]
    public async Task Http_errors_become_in_character_messages(HttpStatusCode status, string expected)
    {
        var server = new FakeServer(status, """{"error":{"message":"nope"}}""");
        var brain = new OpenAICompatibleBrain("Gemini", "https://api.example.com/v1", "m", "k", server);

        var ex = await Assert.ThrowsAsync<BrainException>(() => brain.ReplyAsync("hi", Now, CancellationToken.None));
        Assert.Contains(expected, ex.Message);
        Assert.Contains("(nope)", ex.Message);
    }

    [Fact]
    public async Task Failed_turns_are_not_kept_in_history()
    {
        var failing = new FakeServer(HttpStatusCode.InternalServerError, "{}");
        var brain = new OpenAICompatibleBrain("x", "https://api.example.com/v1", "m", "k", failing);
        await Assert.ThrowsAsync<BrainException>(() => brain.ReplyAsync("lost", Now, CancellationToken.None));
        await Assert.ThrowsAsync<BrainException>(() => brain.ReplyAsync("again", Now, CancellationToken.None));
        var messages = JsonNode.Parse(failing.Requests[1].Body)!["messages"]!.AsArray();
        Assert.Equal(2, messages.Count); // system + the new user turn only
    }

    [Fact]
    public async Task Lists_models_in_openai_shape()
    {
        var server = new FakeServer(HttpStatusCode.OK, """{"data":[{"id":"b-model"},{"id":"models/a-model"}]}""");
        var brain = new OpenAICompatibleBrain("Gemini", "https://api.example.com/v1beta/openai", "m", "k", server);

        var models = await brain.ListModelsAsync(CancellationToken.None);

        Assert.Equal(["a-model", "b-model"], models);
        Assert.Equal("https://api.example.com/v1beta/openai/models", server.Requests.Single().Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task Lists_github_models_from_the_catalog()
    {
        var server = new FakeServer(HttpStatusCode.OK, """[{"id":"openai/gpt-4.1"},{"id":"meta/llama"}]""");
        var brain = new OpenAICompatibleBrain("Copilot", "https://models.github.ai/inference", "openai/gpt-4.1", "ghp_x", server);

        var models = await brain.ListModelsAsync(CancellationToken.None);

        Assert.Equal(["meta/llama", "openai/gpt-4.1"], models);
        var request = server.Requests.Single().Request;
        Assert.Equal("https://models.github.ai/catalog/models", request.RequestUri!.ToString());
        Assert.Contains("2022-11-28", request.Headers.GetValues("X-GitHub-Api-Version"));
    }

    [Fact]
    public async Task Unreachable_local_server_suggests_starting_it()
    {
        // Port 9 (discard) on loopback is closed on practically every machine.
        var brain = new OpenAICompatibleBrain("Ollama", "http://127.0.0.1:9/v1", "llama3.2", null);
        var ex = await Assert.ThrowsAsync<BrainException>(() => brain.ReplyAsync("hi", Now, CancellationToken.None));
        Assert.Contains("running", ex.Message);
    }
}

public class MoodParsingTests
{
    [Theory]
    [InlineData("[smug] Obviously.", "Obviously.", PastyReaction.Smug)]
    [InlineData("*waving* Hi there, it's nice to meet you!", "Hi there, it's nice to meet you!", PastyReaction.Wave)]
    [InlineData("*sighs dramatically* Fine.", "Fine.", PastyReaction.Annoyed)]
    [InlineData("[happy] *grins* Yes!", "Yes!", PastyReaction.Happy)]
    [InlineData("No tag at all.", "No tag at all.", PastyReaction.None)]
    [InlineData("*not closed, so not an action", "*not closed, so not an action", PastyReaction.None)]
    public void Mood_comes_from_tags_or_roleplay_actions(string raw, string text, PastyReaction reaction)
    {
        var parsed = PastyPrompt.ParseMood(raw);
        Assert.Equal(text, parsed.Text);
        Assert.Equal(reaction, parsed.Reaction);
    }
}

public class BrainSettingsTests
{
    [Fact]
    public void Legacy_claude_settings_migrate_into_a_profile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pasty-migrate-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{ "Brain": "Claude", "ClaudeModel": "claude-x", "ProtectedApiKey": "dpapi:abc" }""");
            var settings = new SettingsStore(path).Load();
            Assert.Equal(new BrainProfile("claude-x", null, "dpapi:abc"), settings.ProfileFor(BrainKind.Claude));
            Assert.Null(settings.ClaudeModel);

            new SettingsStore(path).Save(settings);
            Assert.DoesNotContain("ClaudeModel", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Clones_do_not_share_profiles()
    {
        var original = new PastySettings();
        var copy = original.Clone();
        copy.BrainProfiles[BrainKind.OpenAI] = new BrainProfile("gpt-x");
        Assert.False(original.BrainProfiles.ContainsKey(BrainKind.OpenAI));
    }

    [Fact]
    public void Every_provider_is_described() =>
        Assert.All(Enum.GetValues<BrainKind>(), kind => Assert.NotNull(BrainProviders.Get(kind)));
}
