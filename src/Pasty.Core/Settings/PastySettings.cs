namespace Pasty.Core.Settings;

public enum Chattiness
{
    /// <summary>Only speaks when spoken to.</summary>
    Quiet,
    /// <summary>Occasional idle remarks.</summary>
    Polite,
    /// <summary>Regular idle remarks and context reactions.</summary>
    Chatty,
    /// <summary>The full 1997 experience.</summary>
    Relentless,
}

public enum MotionPreference
{
    /// <summary>Follow the operating system's "reduce animations" setting.</summary>
    Auto,
    Reduced,
    Full,
}

public enum BrainKind
{
    Offline,
    Claude,
    GitHubModels,
    OpenAI,
    Gemini,
    Ollama,
    Custom,
}

/// <summary>Per-provider brain settings. Null fields mean "use the provider's default".</summary>
public sealed record BrainProfile(string? Model = null, string? Endpoint = null, string? ProtectedApiKey = null);

/// <summary>
/// Everything the user can configure. Every field that lets Pasty observe the
/// user's machine defaults to off; the privacy contract lives here.
/// </summary>
public sealed class PastySettings
{
    // General
    public bool StartWithWindows { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public double Scale { get; set; } = 1.0;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    // Living with Pasty all day
    /// <summary>Hide while a full-screen app, game or presentation is in front.</summary>
    public bool HideWhenFullScreen { get; set; } = true;
    /// <summary>Doze off after this many minutes without keyboard/mouse input. 0 = never.</summary>
    public int SleepAfterMinutes { get; set; } = 10;
    /// <summary>No unprompted remarks between these times (reminders and answers still come through).</summary>
    public bool QuietHoursEnabled { get; set; }
    public TimeSpan QuietHoursStart { get; set; } = TimeSpan.FromHours(22);
    public TimeSpan QuietHoursEnd { get; set; } = TimeSpan.FromHours(8);
    /// <summary>Calmer animations: no bobbing, hopping or swaying. Auto follows the OS setting.</summary>
    public MotionPreference Motion { get; set; } = MotionPreference.Auto;
    /// <summary>Suggest a break after this many minutes of continuous activity. 0 = off.</summary>
    public int BreakNudgeMinutes { get; set; }
    /// <summary>Global keyboard shortcut that opens the ask box from anywhere ("" = off). Windows only.</summary>
    public string AskHotkey { get; set; } = "Ctrl+Alt+P";

    // Personality
    public Chattiness Chattiness { get; set; } = Chattiness.Polite;
    /// <summary>0 = saintly, 100 = insufferable.</summary>
    public int Sarcasm { get; set; } = 50;
    public bool TimeOfDayGreetings { get; set; } = true;
    /// <summary>Which companion is on the desktop (see Characters.All). Pasty by default.</summary>
    public string Character { get; set; } = Characters.Characters.PastyId;
    /// <summary>Costumes and seasonal lines (Halloween Edition). Auto = when the calendar says so.</summary>
    public Seasons.SeasonalMode Seasonal { get; set; } = Seasons.SeasonalMode.Auto;

    // Awareness: opt-in only
    public bool WatchActiveApp { get; set; }
    public bool WatchClipboard { get; set; }

    // Brain
    public BrainKind Brain { get; set; } = BrainKind.Offline;
    /// <summary>Model, endpoint and protected API key for each provider, so switching back and forth keeps your setup.</summary>
    public Dictionary<BrainKind, BrainProfile> BrainProfiles { get; set; } = [];

    /// <summary>1.0.0 stored the Claude settings flat. Read for migration only; never written.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ClaudeModel { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ProtectedApiKey { get; set; }
    /// <summary>When true, the active app's category (never its window title) is included in AI prompts.</summary>
    public bool ShareContextWithAi { get; set; }

    // Plugins
    public bool EnableExternalPlugins { get; set; }

    public bool FirstRunComplete { get; set; }

    public BrainProfile ProfileFor(BrainKind kind) =>
        BrainProfiles.TryGetValue(kind, out var profile) ? profile : new BrainProfile();

    /// <summary>Profiles are immutable records, so copying the dictionary is enough for an independent clone.</summary>
    public PastySettings Clone()
    {
        var copy = (PastySettings)MemberwiseClone();
        copy.BrainProfiles = new Dictionary<BrainKind, BrainProfile>(BrainProfiles);
        return copy;
    }

    /// <summary>Moves 1.0.0's flat Claude settings into <see cref="BrainProfiles"/>.</summary>
    internal void MigrateLegacy()
    {
        if (ClaudeModel is null && ProtectedApiKey is null) return;
        if (!BrainProfiles.ContainsKey(BrainKind.Claude))
            BrainProfiles[BrainKind.Claude] = new BrainProfile(ClaudeModel, null, ProtectedApiKey);
        ClaudeModel = null;
        ProtectedApiKey = null;
    }
}
