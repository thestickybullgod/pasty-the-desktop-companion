using Pasty.Core.Settings;

namespace Pasty.Core.Wellbeing;

public static class QuietHours
{
    /// <summary>True when <paramref name="now"/> falls in the quiet window. Windows may wrap past midnight (22:00–08:00).</summary>
    public static bool IsQuiet(PastySettings settings, DateTime now)
    {
        if (!settings.QuietHoursEnabled) return false;
        var start = settings.QuietHoursStart;
        var end = settings.QuietHoursEnd;
        var t = now.TimeOfDay;
        if (start == end) return false;
        return start < end ? t >= start && t < end : t >= start || t < end;
    }
}

/// <summary>
/// Notices long stretches of continuous activity and says when a break nudge is due.
/// A pause of <see cref="BreakLength"/> or more (no keyboard or mouse) counts as a break and resets the clock.
/// </summary>
public sealed class BreakTracker
{
    public static readonly TimeSpan BreakLength = TimeSpan.FromMinutes(5);

    private DateTime? _activeSince;
    private DateTime _lastNudge = DateTime.MinValue;

    /// <summary>How long the current active stretch has lasted.</summary>
    public TimeSpan ActiveFor(DateTime now) => _activeSince is { } since ? now - since : TimeSpan.Zero;

    /// <summary>
    /// Updates the tracker and says whether a nudge is due. A due nudge stays due until
    /// <see cref="Nudged"/> is called, so one that couldn't be shown yet isn't lost.
    /// </summary>
    /// <param name="idle">Time since the last input, or null if the OS can't say (then every minute counts as active).</param>
    /// <param name="intervalMinutes">Nudge after this much activity; 0 turns nudges off.</param>
    public bool IsDue(DateTime now, TimeSpan? idle, int intervalMinutes)
    {
        if (intervalMinutes <= 0)
        {
            _activeSince = null;
            return false;
        }

        if (idle is { } i && i >= BreakLength)
        {
            _activeSince = null;   // they took a break on their own: good
            return false;
        }

        _activeSince ??= now - (idle ?? TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        return now - _activeSince >= interval && now - _lastNudge >= interval;
    }

    /// <summary>Records that the nudge was shown; the next one comes a full interval later.</summary>
    public void Nudged(DateTime now) => _lastNudge = now;
}
