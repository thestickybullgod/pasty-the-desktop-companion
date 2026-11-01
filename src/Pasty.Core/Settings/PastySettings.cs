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

public enum BrainKind
{
    Offline,
    Claude,
}

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

    // Personality
    public Chattiness Chattiness { get; set; } = Chattiness.Polite;
    /// <summary>0 = saintly, 100 = insufferable.</summary>
    public int Sarcasm { get; set; } = 50;
    public bool TimeOfDayGreetings { get; set; } = true;
    /// <summary>Costumes and seasonal lines (Halloween Edition). Auto = when the calendar says so.</summary>
    public Seasons.SeasonalMode Seasonal { get; set; } = Seasons.SeasonalMode.Auto;

    // Awareness: opt-in only
    public bool WatchActiveApp { get; set; }
    public bool WatchClipboard { get; set; }

    // Brain
    public BrainKind Brain { get; set; } = BrainKind.Offline;
    public string ClaudeModel { get; set; } = "claude-opus-5-5";
    /// <summary>Opaque token from the platform secret store (DPAPI on Windows, keyring on Linux).</summary>
    public string? ProtectedApiKey { get; set; }
    /// <summary>When true, the active app's category (never its window title) is included in AI prompts.</summary>
    public bool ShareContextWithAi { get; set; }

    // Plugins
    public bool EnableExternalPlugins { get; set; }

    public bool FirstRunComplete { get; set; }

    public PastySettings Clone() => (PastySettings)MemberwiseClone();
}
