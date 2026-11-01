using Avalonia.Threading;
using Pasty.Core.Platform;

namespace Pasty.Desktop.Services;

/// <summary>
/// Every few seconds, asks the OS how long since the last input and whether something is full-screen.
/// No hooks, no input capture: just the OS's own counters.
/// </summary>
internal sealed class PresenceMonitor : IDisposable
{
    private readonly IPlatformServices _platform;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(4) };
    private DateTime? _awaySince;

    public PresenceMonitor(IPlatformServices platform)
    {
        _platform = platform;
        _timer.Tick += (_, _) => Poll();
    }

    /// <summary>Inactivity before the user counts as away. Zero disables.</summary>
    public TimeSpan AwayAfter { get; set; } = TimeSpan.FromMinutes(10);

    public bool IsAway { get; private set; }

    public bool IsFullScreenActive { get; private set; }

    /// <summary>Raised when the user comes back, with how long they were gone.</summary>
    public event Action<TimeSpan>? Returned;

    public event Action? WentAway;

    public event Action? FullScreenChanged;

    public void Start()
    {
        _timer.Start();
        Poll();
    }

    private void Poll()
    {
        var idle = _platform.GetIdleTime();
        var away = idle is { } i && AwayAfter > TimeSpan.Zero && i >= AwayAfter;
        if (away && !IsAway)
        {
            IsAway = true;
            _awaySince = DateTime.Now - idle!.Value;
            WentAway?.Invoke();
        }
        else if (!away && IsAway)
        {
            IsAway = false;
            Returned?.Invoke(_awaySince is { } since ? DateTime.Now - since : TimeSpan.Zero);
            _awaySince = null;
        }

        var fullScreen = _platform.IsSomethingFullScreen();
        if (fullScreen != IsFullScreenActive)
        {
            IsFullScreenActive = fullScreen;
            FullScreenChanged?.Invoke();
        }
    }

    public void Dispose() => _timer.Stop();
}
