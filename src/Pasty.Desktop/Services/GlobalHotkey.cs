using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;

namespace Pasty.Desktop.Services;

/// <summary>
/// A system-wide keyboard shortcut (e.g. Ctrl+Alt+P) that works whichever app has focus.
/// Windows only: registered with RegisterHotKey on one of Pasty's own windows.
/// </summary>
internal sealed class GlobalHotkey : IDisposable
{
    public static readonly string[] Presets = ["Ctrl+Alt+P", "Ctrl+Alt+Space", "Ctrl+Shift+Alt+P"];

    private const int HotkeyId = 0x5041; // "PA"
    private const int WM_HOTKEY = 0x0312;

    private readonly Window _owner;
    private readonly Win32Properties.CustomWndProcHookCallback _hook;
    private IntPtr _hwnd;
    private bool _hooked;

    public GlobalHotkey(Window owner)
    {
        _owner = owner;
        _hook = OnMessage;
    }

    public event Action? Pressed;

    /// <summary>The registered combination, or null when none is active.</summary>
    public string? Active { get; private set; }

    /// <summary>Why the last registration failed, for the Settings page; null when fine or off.</summary>
    public string? Problem { get; private set; }

    /// <summary>Registers <paramref name="combo"/> ("" or null turns the shortcut off).</summary>
    public void Set(string? combo)
    {
        if (!OperatingSystem.IsWindows())
        {
            Problem = string.IsNullOrEmpty(combo) ? null : "Global shortcuts are only available on Windows for now.";
            return;
        }
        if (combo == Active && Problem is null) return;
        Unregister();
        Problem = null;
        if (string.IsNullOrWhiteSpace(combo)) return;

        if (!TryParse(combo, out var modifiers, out var key))
        {
            Problem = $"I don't understand the shortcut \"{combo}\".";
            return;
        }

        _hwnd = _owner.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (_hwnd == IntPtr.Zero)
        {
            Problem = "Pasty's window isn't ready yet.";
            return;
        }
        if (!_hooked)
        {
            Win32Properties.AddWndProcHookCallback(_owner, _hook);
            _hooked = true;
        }

        if (RegisterHotKey(_hwnd, HotkeyId, modifiers | MOD_NOREPEAT, key))
            Active = combo;
        else
            Problem = $"{combo} is already used by another app. Pick a different shortcut.";
    }

    private IntPtr OnMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    internal static bool TryParse(string combo, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;
        foreach (var part in combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": case "control": modifiers |= MOD_CONTROL; break;
                case "alt": modifiers |= MOD_ALT; break;
                case "shift": modifiers |= MOD_SHIFT; break;
                case "win": modifiers |= MOD_WIN; break;
                case "space": key = 0x20; break;
                default:
                    if (part.Length == 1 && char.IsLetterOrDigit(part[0])) key = char.ToUpperInvariant(part[0]);
                    else return false;
                    break;
            }
        }
        return key != 0 && modifiers != 0;
    }

    private void Unregister()
    {
        if (Active is not null && _hwnd != IntPtr.Zero && OperatingSystem.IsWindows())
            UnregisterHotKey(_hwnd, HotkeyId);
        Active = null;
    }

    public void Dispose()
    {
        Unregister();
        if (_hooked) Win32Properties.RemoveWndProcHookCallback(_owner, _hook);
    }

    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
