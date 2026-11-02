using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Pasty.Core;
using Pasty.Core.Brains;

namespace Pasty.Brains.Claude;

/// <summary>Optional AI brain backed by the Claude API. Pasty works fine without it.</summary>
public sealed class ClaudeBrain : IPastyBrain
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);
    private const int MaxTurnsBeforeReset = 40;

    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly string _systemPrompt;
    private readonly List<BetaMessageParam> _history = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <param name="systemPrompt">Personality prompt for the selected character; defaults to Pasty.</param>
    public ClaudeBrain(string apiKey, string model, string? systemPrompt = null)
    {
        _systemPrompt = systemPrompt ?? PastyPrompt.System;
        _client = new AnthropicClient { ApiKey = apiKey };
        _model = model;
    }

    public string Name => "Claude";

    public string Model => _model;

    public bool IsRemote => true;

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

            _history.Add(new BetaUserMessageParam { Content = PastyPrompt.WithContext(userText, context) });

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);

            BetaMessage response;
            try
            {
                response = await _client.Beta.Messages.Create(new MessageCreateParams
                {
                    Model = _model,
                    MaxTokens = 2048,
                    System = _systemPrompt,
                    // Chat banter doesn't need deep reasoning; low effort keeps replies snappy and cheap.
                    OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
                    // If a safety classifier declines, let the API re-serve the request on a fallback model.
                    Betas = ["server-side-fallback-2026-07-01"],
                    Fallbacks = new Default(),
                    Messages = [.. _history],
                }, timeout.Token);
            }
            catch
            {
                // Keep history append-only and consistent: drop the unanswered user turn.
                _history.RemoveAt(_history.Count - 1);
                throw;
            }

            _history.Add(new BetaAssistantMessageParam { Content = ToParams(response.Content) });

            if (response.StopReason == "refusal")
                return new Utterance("I'd rather not help with that one. Ask me something else?", PastyReaction.Annoyed);

            var text = string.Concat(response.Content
                .Select(b => b.TryPickText(out var t) ? t.Text : "")).Trim();
            return PastyPrompt.ParseMood(text);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            throw new BrainException("My AI brain says that API key isn't valid. Check Settings → Brain.", ex);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new BrainException("My AI brain is rate-limited. Give me a minute to catch my breath.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BrainException("My AI brain took too long to answer. Try again?");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Round-trips every block (including thinking) so the history stays valid for the API.</summary>
    private static List<BetaContentBlockParam> ToParams(IReadOnlyList<BetaContentBlock> blocks)
    {
        List<BetaContentBlockParam> result = [];
        foreach (var block in blocks)
        {
            if (block.TryPickText(out var text))
                result.Add(new BetaTextBlockParam { Text = text.Text });
            else if (block.TryPickThinking(out var thinking))
                result.Add(new BetaThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
            else if (block.TryPickRedactedThinking(out var redacted))
                result.Add(new BetaRedactedThinkingBlockParam { Data = redacted.Data });
        }
        return result;
    }

}
