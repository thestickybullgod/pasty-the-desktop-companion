using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using Pasty.Core.Platform;

namespace Pasty.Desktop.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsPlatform : IPlatformServices
{
    public string Name => "Windows";

    public IReadOnlyList<string> Limitations => [];

    public bool SupportsActiveAppAwareness => true;

    /// <summary>
    /// Store (MSIX) installs can't use the Run key: Windows virtualizes registry writes for packaged
    /// apps, so the entry would silently do nothing. They declare a startup task in the package instead.
    /// </summary>
    public IStartupRegistration Startup { get; } =
#if WINDOWS10_0_17763_0_OR_GREATER
        IsPackaged && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) ? new PackagedStartupTask() :
#endif
        new RunKeyStartup();

    /// <summary>True when running from an MSIX package (e.g. installed from the Microsoft Store).</summary>
    public static bool IsPackaged
    {
        get
        {
            const int AppModelErrorNoPackage = 15700;
            var length = 0;
            return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage;
        }
    }

    /// <summary>True when Windows started Pasty through its packaged startup task (i.e. at sign-in).</summary>
    public static bool LaunchedByStartupTask()
    {
#if WINDOWS10_0_17763_0_OR_GREATER
        if (!IsPackaged || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return false;
        try
        {
            return Windows.ApplicationModel.AppInstance.GetActivatedEventArgs()?.Kind
                == Windows.ApplicationModel.Activation.ActivationKind.StartupTask;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            return false;
        }
#else
        return false;
#endif
    }

    public ISecretStore Secrets { get; } = new DpapiSecretStore();

    public TimeSpan? GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return null;
        // Both are 32-bit tick counts; unsigned subtraction handles wrap-around.
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime));
    }

    public bool IsSomethingFullScreen()
    {
        if (SHQueryUserNotificationState(out var state) != 0) return false;
        // QUNS_BUSY, QUNS_RUNNING_D3D_FULL_SCREEN, QUNS_PRESENTATION_MODE
        return state is 2 or 3 or 4;
    }

    public (int X, int Y)? GetCursorPosition() => GetCursorPos(out var p) ? (p.X, p.Y) : null;

    public string? GetForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return null;
        GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    public long? GetClipboardChangeStamp() => GetClipboardSequenceNumber();

    /// <summary>Keep Pasty out of Alt+Tab and Task View; it's a companion, not a window you switch to.</summary>
    public static void HideFromAltTab(IntPtr hwnd)
    {
        const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, WS_EX_APPWINDOW = 0x40000;
        var style = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)((style | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW));
    }

    private sealed class RunKeyStartup : IStartupRegistration
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue("Pasty") is string;
        }

        public void Set(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled && Environment.ProcessPath is { } exe)
                key.SetValue("Pasty", $"\"{exe}\" {Program.StartupArgument}");
            else
                key.DeleteValue("Pasty", throwOnMissingValue: false);
        }
    }

#if WINDOWS10_0_17763_0_OR_GREATER
    /// <summary>
    /// The startup task declared in the package manifest (TaskId "PastyStartup"). Users can also
    /// switch it in Windows Settings → Apps → Startup; when they've disabled it there, Windows won't
    /// let Pasty re-enable it, and the toggle in Pasty's settings will simply stay off.
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")]
    private sealed class PackagedStartupTask : IStartupRegistration
    {
        private const string TaskId = "PastyStartup";

        public bool IsEnabled() => State() is Windows.ApplicationModel.StartupTaskState.Enabled
            or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;

        public void Set(bool enabled) => Run(async () =>
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync(TaskId);
            if (enabled) await task.RequestEnableAsync();
            else task.Disable();
            return 0;
        });

        private static Windows.ApplicationModel.StartupTaskState State() =>
            Run(async () => (await Windows.ApplicationModel.StartupTask.GetAsync(TaskId)).State);

        // WinRT async calls run off the UI thread so blocking on them can't deadlock it.
        private static T Run<T>(Func<Task<T>> work) => Task.Run(work).GetAwaiter().GetResult();
    }
#endif

    private sealed class DpapiSecretStore : ISecretStore
    {
        private static readonly byte[] Entropy = "Pasty has entered the chat."u8.ToArray();

        public string Description => "Encrypted with Windows DPAPI; only your Windows account can read it.";

        // Each DPAPI blob is self-contained, so the slot needs no special handling.
        public string Protect(string secret, string slot) =>
            "dpapi:" + Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser));

        public string? Unprotect(string? protectedSecret)
        {
            if (protectedSecret is null || !protectedSecret.StartsWith("dpapi:")) return null;
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedSecret[6..]), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern int GetCurrentPackageFullName(ref int length, char[]? name);
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    [DllImport("shell32.dll")] private static extern int SHQueryUserNotificationState(out int state);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
