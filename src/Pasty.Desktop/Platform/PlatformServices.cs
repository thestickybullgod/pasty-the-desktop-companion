using Pasty.Core.Platform;

namespace Pasty.Desktop.Platform;

internal static class PlatformServices
{
    public static IPlatformServices Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsPlatform();
        if (OperatingSystem.IsLinux()) return new LinuxPlatform();
        return new FallbackPlatform();
    }

    /// <summary>Anything else Avalonia runs on (e.g. macOS): Pasty works, just without OS awareness.</summary>
    private sealed class FallbackPlatform : IPlatformServices, IStartupRegistration, ISecretStore
    {
        public string Name => Environment.OSVersion.Platform.ToString();
        public IReadOnlyList<string> Limitations { get; } =
            ["This OS isn't fully supported yet: no idle detection, app awareness, eye tracking or start-on-login."];
        public bool SupportsActiveAppAwareness => false;
        public bool PrefersReducedMotion => false;
        public IStartupRegistration Startup => this;
        public ISecretStore Secrets => this;
        public TimeSpan? GetIdleTime() => null;
        public bool IsSomethingFullScreen() => false;
        public (int X, int Y)? GetCursorPosition() => null;
        public string? GetForegroundProcessName() => null;
        public long? GetClipboardChangeStamp() => null;

        bool IStartupRegistration.IsEnabled() => false;
        void IStartupRegistration.Set(bool enabled) { }

        string ISecretStore.Description => "Stored in Pasty's settings file, readable only by your user.";
        string ISecretStore.Protect(string secret, string slot) => "plain:" + secret;
        string? ISecretStore.Unprotect(string? s) => s is not null && s.StartsWith("plain:") ? s[6..] : null;
    }
}
