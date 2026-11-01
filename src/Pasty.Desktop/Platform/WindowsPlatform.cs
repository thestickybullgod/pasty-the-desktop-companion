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

    public IStartupRegistration Startup { get; } = new RunKeyStartup();

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

    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    [DllImport("shell32.dll")] private static extern int SHQueryUserNotificationState(out int state);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
