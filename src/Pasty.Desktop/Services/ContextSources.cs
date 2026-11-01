using Avalonia.Input.Platform;
using Avalonia.Threading;
using Pasty.Core.Context;
using Pasty.Core.Platform;
using Pasty.Core.Settings;

namespace Pasty.Desktop.Services;

/// <summary>
/// Notices which kind of app is in the foreground. Reads only the process name, reduces it to a
/// category on the spot, and never looks at window titles. Polls a cheap OS call every 2 seconds.
/// </summary>
internal sealed class ActiveAppSource(IPlatformServices platform) : IContextSource
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private string? _lastCategory;
    private bool _wired;

    public string Id => "active-app";

    public string Description => platform.SupportsActiveAppAwareness
        ? "Which kind of app you switch to (e.g. \"code editor\"), from its process name only"
        : "Which kind of app you switch to (not available on this system)";

    public event Action<ContextSignal>? Signal;

    public string? CurrentCategory => _timer.IsEnabled ? _lastCategory : null;

    public bool IsConsented(PastySettings settings) => settings.WatchActiveApp && platform.SupportsActiveAppAwareness;

    public void Start()
    {
        if (!_wired)
        {
            _timer.Tick += (_, _) => Poll();
            _wired = true;
        }
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _lastCategory = null;
    }

    private void Poll()
    {
        var name = platform.GetForegroundProcessName();
        if (name is null || name.Equals("Pasty", StringComparison.OrdinalIgnoreCase)) return;
        var category = AppClassifier.Classify(name);
        if (category == _lastCategory) return;
        _lastCategory = category;
        if (category != AppClassifier.Other)
            Signal?.Invoke(new ContextSignal(ContextKind.ActiveApp, category));
    }

    public void Dispose() => Stop();
}

/// <summary>
/// Notices when you copy text and what *kind* it was (link, code, number...). The text is
/// classified in memory and dropped; only a hash is kept, to tell whether the clipboard changed.
/// </summary>
internal sealed class ClipboardSource(IPlatformServices platform, Func<IClipboard?> clipboard) : IContextSource
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private long? _lastStamp;
    private int? _lastHash;
    private bool _wired;
    private bool _busy;

    public string Id => "clipboard";

    public string Description => "When you copy text, and what kind it is (link, code, number). The text itself is never kept or sent";

    public event Action<ContextSignal>? Signal;

    public bool IsConsented(PastySettings settings) => settings.WatchClipboard;

    public void Start()
    {
        if (!_wired)
        {
            _timer.Tick += async (_, _) => await PollAsync();
            _wired = true;
        }
        _lastStamp = platform.GetClipboardChangeStamp();
        _lastHash = null;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private async Task PollAsync()
    {
        if (_busy) return;

        // Windows has a change counter, so the clipboard is only read when something was copied.
        var stamp = platform.GetClipboardChangeStamp();
        if (stamp is not null && stamp == _lastStamp) return;
        _lastStamp = stamp;

        if (clipboard() is not { } cb) return;
        _busy = true;
        try
        {
            var text = await cb.TryGetTextAsync();
            if (string.IsNullOrEmpty(text)) return;

            var hash = text.GetHashCode();
            var first = _lastHash is null;
            if (hash == _lastHash) return;
            _lastHash = hash;
            // The first read only establishes a baseline; don't react to whatever was copied before.
            if (!first || stamp is not null)
                Signal?.Invoke(new ContextSignal(ContextKind.Clipboard, ClipboardClassifier.Classify(text)));
        }
        catch (Exception)
        {
            // Another app has the clipboard locked, or it holds something exotic. Not our business.
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose() => Stop();
}
