using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pasty.Core.Platform;

namespace Pasty.Desktop.Platform;

/// <summary>
/// Linux via X11 (including XWayland). Talks to libX11/libXss directly so there are no extra
/// packages to install. Anything X11 can't answer degrades to "unknown" rather than failing.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxPlatform : IPlatformServices, IDisposable
{
    private const long XA_ATOM = 4, XA_CARDINAL = 6, XA_WINDOW = 33;

    private readonly IntPtr _display;
    private readonly IntPtr _root;
    private readonly bool _hasScreenSaverExt;
    private readonly List<string> _limitations = [];
    private readonly XErrorHandler? _errorHandler;
    private IntPtr _atomActiveWindow, _atomPid, _atomState, _atomFullscreen;

    public LinuxPlatform()
    {
        var wayland = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland"
            || Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is not null;

        try
        {
            _display = XOpenDisplay(IntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
            _display = IntPtr.Zero;
        }

        if (_display != IntPtr.Zero)
        {
            // Windows can vanish between our calls; Xlib's default handler would exit the process over that.
            _errorHandler = (_, _) => 0;
            XSetErrorHandler(_errorHandler);
            _root = XDefaultRootWindow(_display);
            _atomActiveWindow = XInternAtom(_display, "_NET_ACTIVE_WINDOW", false);
            _atomPid = XInternAtom(_display, "_NET_WM_PID", false);
            _atomState = XInternAtom(_display, "_NET_WM_STATE", false);
            _atomFullscreen = XInternAtom(_display, "_NET_WM_STATE_FULLSCREEN", false);
            try
            {
                _hasScreenSaverExt = XScreenSaverQueryExtension(_display, out _, out _);
            }
            catch (DllNotFoundException)
            {
                _limitations.Add("Napping when you're away needs libXss (package libxss1 / libXScrnSaver).");
            }
        }
        else
        {
            _limitations.Add("No X11 display found, so idle detection, app awareness and eye tracking are off.");
        }

        if (wayland)
        {
            _limitations.Add("Wayland session: Pasty runs through XWayland. It can only see native-Wayland apps' "
                + "focus and the cursor while they're over XWayland windows, and your compositor may ignore 'always on top'.");
        }

        Name = wayland ? "Linux (Wayland via XWayland)" : "Linux (X11)";
        SupportsActiveAppAwareness = _display != IntPtr.Zero;
    }

    public string Name { get; }

    public IReadOnlyList<string> Limitations => _limitations;

    public bool SupportsActiveAppAwareness { get; }

    public IStartupRegistration Startup { get; } = new XdgAutostart();

    public ISecretStore Secrets { get; } = new LinuxSecretStore();

    public TimeSpan? GetIdleTime()
    {
        if (!_hasScreenSaverExt) return null;
        var info = XScreenSaverAllocInfo();
        if (info == IntPtr.Zero) return null;
        try
        {
            if (XScreenSaverQueryInfo(_display, _root, info) == 0) return null;
            // struct XScreenSaverInfo { Window window; int state; int kind; unsigned long til_or_since; unsigned long idle; ... }
            var idleMs = Marshal.ReadInt64(info, IntPtr.Size + 4 + 4 + IntPtr.Size);
            return TimeSpan.FromMilliseconds(idleMs);
        }
        finally
        {
            XFree(info);
        }
    }

    public bool IsSomethingFullScreen()
    {
        var window = ActiveWindow();
        if (window == IntPtr.Zero) return false;
        var atoms = GetLongProperty(window, _atomState, XA_ATOM, 64);
        return atoms.Contains((long)_atomFullscreen);
    }

    public (int X, int Y)? GetCursorPosition()
    {
        if (_display == IntPtr.Zero) return null;
        return XQueryPointer(_display, _root, out _, out _, out var x, out var y, out _, out _, out _) ? (x, y) : null;
    }

    public string? GetForegroundProcessName()
    {
        var window = ActiveWindow();
        if (window == IntPtr.Zero) return null;
        var pid = GetLongProperty(window, _atomPid, XA_CARDINAL, 1);
        if (pid.Length == 0) return null;
        try
        {
            return File.ReadAllText($"/proc/{pid[0]}/comm").Trim();
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>X11 has no cheap change counter without an event loop; the clipboard source falls back to comparing hashes.</summary>
    public long? GetClipboardChangeStamp() => null;

    private IntPtr ActiveWindow()
    {
        if (_display == IntPtr.Zero) return IntPtr.Zero;
        var values = GetLongProperty(_root, _atomActiveWindow, XA_WINDOW, 1);
        return values.Length > 0 ? (IntPtr)values[0] : IntPtr.Zero;
    }

    private long[] GetLongProperty(IntPtr window, IntPtr property, long type, long maxItems)
    {
        var status = XGetWindowProperty(_display, window, property, 0, maxItems, false, (IntPtr)type,
            out _, out var format, out var count, out _, out var data);
        if (status != 0 || data == IntPtr.Zero) return [];
        try
        {
            // Xlib returns 32-bit-format properties as C longs (8 bytes on 64-bit Linux).
            if (format != 32) return [];
            var result = new long[(int)count];
            for (var i = 0; i < result.Length; i++)
                result[i] = Marshal.ReadInt64(data, i * 8);
            return result;
        }
        finally
        {
            XFree(data);
        }
    }

    public void Dispose()
    {
        if (_display != IntPtr.Zero) XCloseDisplay(_display);
    }

    /// <summary>Freedesktop autostart: ~/.config/autostart/pasty.desktop.</summary>
    private sealed class XdgAutostart : IStartupRegistration
    {
        private static string FilePath
        {
            get
            {
                var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(config))
                    config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                return Path.Combine(config, "autostart", "pasty.desktop");
            }
        }

        public bool IsEnabled() => File.Exists(FilePath);

        public void Set(bool enabled)
        {
            if (!enabled)
            {
                File.Delete(FilePath);
                return;
            }
            if (Environment.ProcessPath is not { } exe) return;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, $"""
                [Desktop Entry]
                Type=Application
                Name=Pasty
                Comment=Your desktop companion
                Exec="{exe}" {Program.StartupArgument}
                X-GNOME-Autostart-enabled=true
                """);
        }
    }

    /// <summary>
    /// Uses the desktop keyring via libsecret's <c>secret-tool</c> when installed;
    /// otherwise the key stays in the settings file, which is created readable by your user only.
    /// </summary>
    private sealed class LinuxSecretStore : ISecretStore
    {
        private const string KeyringMarker = "keyring:pasty";

        /// <summary>One keyring entry per slot (provider). 1.0.0's single entry had no slot suffix.</summary>
        private static string[] Lookup(string? slot) =>
            ["service", "pasty", "account", slot is null ? "api-key" : "api-key-" + slot];

        private readonly bool _hasSecretTool = Run("secret-tool", ["--version"], null, out _);

        public string Description => _hasSecretTool
            ? "Stored in your desktop keyring (libsecret)."
            : "Stored in Pasty's settings file, readable only by your user. Install libsecret-tools to use your keyring instead.";

        public string Protect(string secret, string slot)
        {
            if (_hasSecretTool && Run("secret-tool", ["store", $"--label=Pasty API key ({slot})", .. Lookup(slot)], secret, out _))
                return KeyringMarker + ":" + slot;
            return "plain:" + secret;
        }

        public string? Unprotect(string? protectedSecret)
        {
            if (protectedSecret is not null && protectedSecret.StartsWith(KeyringMarker))
            {
                var slot = protectedSecret.Length > KeyringMarker.Length ? protectedSecret[(KeyringMarker.Length + 1)..] : null;
                return Run("secret-tool", ["lookup", .. Lookup(slot)], null, out var output) ? output.Trim() : null;
            }
            return protectedSecret is not null && protectedSecret.StartsWith("plain:") ? protectedSecret[6..] : null;
        }

        private static bool Run(string tool, string[] args, string? input, out string output)
        {
            output = "";
            try
            {
                var start = new ProcessStartInfo(tool)
                {
                    RedirectStandardInput = input is not null,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                foreach (var a in args) start.ArgumentList.Add(a);
                using var process = Process.Start(start)!;
                if (input is not null)
                {
                    process.StandardInput.Write(input);
                    process.StandardInput.Close();
                }
                output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(5000)) return false;
                return process.ExitCode == 0;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                return false;
            }
        }
    }

    private delegate int XErrorHandler(IntPtr display, IntPtr errorEvent);

    private const string LibX11 = "libX11.so.6";
    private const string LibXss = "libXss.so.1";

    [DllImport(LibX11)] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport(LibX11)] private static extern int XCloseDisplay(IntPtr display);
    [DllImport(LibX11)] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(LibX11)] private static extern IntPtr XSetErrorHandler(XErrorHandler handler);
    [DllImport(LibX11)] private static extern IntPtr XInternAtom(IntPtr display, string name, bool onlyIfExists);
    [DllImport(LibX11)] private static extern int XFree(IntPtr data);

    [DllImport(LibX11)]
    private static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, long offset, long length,
        bool delete, IntPtr requestedType, out IntPtr actualType, out int actualFormat, out ulong itemCount,
        out ulong bytesAfter, out IntPtr data);

    [DllImport(LibX11)]
    private static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
        out int rootX, out int rootY, out int winX, out int winY, out uint mask);

    [DllImport(LibXss)] private static extern bool XScreenSaverQueryExtension(IntPtr display, out int eventBase, out int errorBase);
    [DllImport(LibXss)] private static extern IntPtr XScreenSaverAllocInfo();
    [DllImport(LibXss)] private static extern int XScreenSaverQueryInfo(IntPtr display, IntPtr drawable, IntPtr info);
}
