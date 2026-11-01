using Pasty.Core.Settings;

namespace Pasty.Core.Brains;

/// <summary>Everything the UI and the factory need to know about a brain choice.</summary>
public sealed record BrainProvider(
    BrainKind Kind,
    string DisplayName,
    string Description,
    string? DefaultEndpoint,
    string? DefaultModel,
    bool NeedsApiKey,
    string? ApiKeyEnvironmentVariable,
    string? KeyHelp,
    string PrivacyNote,
    bool EndpointEditable)
{
    /// <summary>What Pasty calls it in conversation: "my Gemini brain".</summary>
    public string ShortName => Kind switch
    {
        BrainKind.GitHubModels => "Copilot",
        BrainKind.OpenAI => "ChatGPT",
        BrainKind.Custom => "custom",
        _ => Kind.ToString(),
    };

    /// <summary>True for providers spoken to via the OpenAI-style chat/completions API.</summary>
    public bool IsOpenAiCompatible => Kind is not (BrainKind.Offline or BrainKind.Claude);
}

public static class BrainProviders
{
    private const string Sent = "What you type into the ask box is sent to ";

    public static readonly IReadOnlyList<BrainProvider> All =
    [
        new(BrainKind.Offline, "Offline (built in)",
            "Pasty's own brain: banter, tips and commands. No network, ever.",
            null, null, false, null, null,
            "Nothing leaves your computer.", false),

        new(BrainKind.Claude, "Claude (Anthropic)",
            "Anthropic's Claude models.",
            null, "claude-opus-5-5", true, "ANTHROPIC_API_KEY",
            "Create a key at console.anthropic.com.",
            Sent + "Anthropic's API.", false),

        new(BrainKind.GitHubModels, "Copilot (GitHub Models)",
            "Models from the GitHub Models catalog (OpenAI, Microsoft, Meta, Mistral and more) with your GitHub account. "
            + "Model IDs look like publisher/model.",
            "https://models.github.ai/inference", "openai/gpt-4.1", true, "GITHUB_TOKEN",
            "Use a GitHub personal access token with the models:read permission (github.com → Settings → Developer settings).",
            Sent + "GitHub Models.", false),

        new(BrainKind.OpenAI, "ChatGPT (OpenAI)",
            "OpenAI's GPT models via the OpenAI API (separate from a ChatGPT subscription).",
            "https://api.openai.com/v1", "gpt-5-mini", true, "OPENAI_API_KEY",
            "Create a key at platform.openai.com.",
            Sent + "OpenAI's API.", false),

        new(BrainKind.Gemini, "Gemini (Google)",
            "Google's Gemini models via Google's OpenAI-compatible endpoint.",
            "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-flash-latest", true, "GEMINI_API_KEY",
            "Create a key at aistudio.google.com.",
            Sent + "Google's Gemini API.", false),

        new(BrainKind.Ollama, "Ollama (runs on your PC)",
            "Open models running locally with Ollama. Private and works without internet once a model is downloaded.",
            "http://localhost:11434/v1", "llama3.2", false, null,
            "No key needed. Install Ollama from ollama.com, then run e.g. 'ollama pull llama3.2'.",
            "Runs on your computer; nothing leaves it (unless you point the endpoint elsewhere).", true),

        new(BrainKind.Custom, "Custom (OpenAI-compatible)",
            "Any OpenAI-compatible endpoint: LM Studio, Azure OpenAI / Microsoft Foundry, OpenRouter, Mistral, Groq…",
            "http://localhost:1234/v1", null, false, null,
            "Key is optional; leave it empty for local servers like LM Studio.",
            Sent + "the endpoint you configure.", true),
    ];

    public static BrainProvider Get(BrainKind kind) => All.First(p => p.Kind == kind);
}
