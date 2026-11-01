namespace Pasty.Core.Platform;

/// <summary>Things the OS knows that Pasty needs. Each method degrades to a harmless default when unsupported.</summary>
public interface IPlatformServices
{
    /// <summary>Human-readable name, e.g. "Windows" or "Linux (X11)".</summary>
    string Name { get; }

    /// <summary>Notes about what doesn't work here, shown in Settings → About. Empty if everything works.</summary>
    IReadOnlyList<string> Limitations { get; }

    /// <summary>Time since the last keyboard/mouse input, or null if the OS won't say.</summary>
    TimeSpan? GetIdleTime();

    /// <summary>True while a full-screen app, game or presentation owns the screen.</summary>
    bool IsSomethingFullScreen();

    /// <summary>Global cursor position in physical pixels, or null if unavailable (e.g. Wayland).</summary>
    (int X, int Y)? GetCursorPosition();

    /// <summary>Process name (no path, no extension) of the foreground app, or null if unavailable.</summary>
    string? GetForegroundProcessName();

    /// <summary>A number that changes whenever the clipboard changes, or null if the OS can't tell cheaply.</summary>
    long? GetClipboardChangeStamp();

    bool SupportsActiveAppAwareness { get; }

    IStartupRegistration Startup { get; }

    ISecretStore Secrets { get; }
}

public interface IStartupRegistration
{
    bool IsEnabled();
    void Set(bool enabled);
}

/// <summary>Stores the AI API key. Settings only ever hold the opaque string this returns.</summary>
public interface ISecretStore
{
    /// <summary>One sentence describing how secrets are protected on this OS.</summary>
    string Description { get; }

    string Protect(string secret);
    string? Unprotect(string? protectedSecret);
}
