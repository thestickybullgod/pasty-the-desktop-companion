using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pasty.Core.Reminders;

public sealed record Reminder(DateTime Due, string Text);

/// <summary>
/// Understands "20m stretch", "1h30m call mom", "in 20 minutes to stretch", "at 3pm call mom",
/// "at 15:30 stand-up". Times in the past roll over to tomorrow.
/// </summary>
public static partial class ReminderParser
{
    public static bool TryParse(string input, DateTime now, out Reminder reminder, out string error)
    {
        reminder = null!;
        var text = input.Trim();

        // "at 3pm ...", "at 15:30 ..."
        var at = AtPattern().Match(text);
        if (at.Success)
        {
            var hour = int.Parse(at.Groups["h"].Value, CultureInfo.InvariantCulture);
            var minute = at.Groups["m"].Success ? int.Parse(at.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
            var meridiem = at.Groups["ap"].Value.ToLowerInvariant();
            if (meridiem == "pm" && hour < 12) hour += 12;
            if (meridiem == "am" && hour == 12) hour = 0;
            if (hour > 23 || minute > 59)
            {
                error = "That time doesn't exist. Try something like \"at 3pm stretch\" or \"at 15:30 stretch\".";
                return false;
            }
            var due = now.Date.AddHours(hour).AddMinutes(minute);
            if (due <= now) due = due.AddDays(1);
            return Finish(due, text[at.Length..], out reminder, out error);
        }

        // "20m ...", "1h30m ...", "in 20 minutes ...", "90 sec ..."
        var span = DurationPattern().Match(text);
        if (span.Success)
        {
            var total = TimeSpan.Zero;
            foreach (Capture c in span.Groups["part"].Captures)
            {
                var m = PartPattern().Match(c.Value);
                var amount = double.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
                total += char.ToLowerInvariant(m.Groups["u"].Value[0]) switch
                {
                    'h' => TimeSpan.FromHours(amount),
                    's' => TimeSpan.FromSeconds(amount),
                    _ => TimeSpan.FromMinutes(amount),
                };
            }
            if (total <= TimeSpan.Zero || total > TimeSpan.FromDays(7))
            {
                error = "I can set reminders from a few seconds up to a week ahead.";
                return false;
            }
            return Finish(now + total, text[span.Length..], out reminder, out error);
        }

        error = "Tell me when, like \"/remind 20m stretch\" or \"/remind at 3pm call mom\".";
        return false;
    }

    private static bool Finish(DateTime due, string rest, out Reminder reminder, out string error)
    {
        var what = ToPattern().Replace(rest.Trim(), "").Trim();
        if (what.Length == 0) what = "This is your reminder. You didn't say what for. Mysterious.";
        reminder = new Reminder(due, what);
        error = "";
        return true;
    }

    [GeneratedRegex(@"^(?:at\s+)(?<h>\d{1,2})(?::(?<m>\d{2}))?\s*(?<ap>am|pm)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex AtPattern();

    [GeneratedRegex(@"^(?:in\s+)?(?:(?<part>\d+(?:\.\d+)?\s*(?:hours?|hrs?|h|minutes?|mins?|m|seconds?|secs?|s))\s*(?:and\s+)?)+(?=\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex DurationPattern();

    [GeneratedRegex(@"(?<n>\d+(?:\.\d+)?)\s*(?<u>[hms])", RegexOptions.IgnoreCase)]
    private static partial Regex PartPattern();

    [GeneratedRegex(@"^(?:to|that|about)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex ToPattern();
}

/// <summary>Pending reminders, kept in a small JSON file so they survive restarts.</summary>
public sealed class ReminderBook
{
    private readonly string? _path;
    private readonly List<Reminder> _items = [];
    private readonly Lock _gate = new();

    public ReminderBook(string? path)
    {
        _path = path;
        try
        {
            if (path is not null && File.Exists(path))
                _items.AddRange(JsonSerializer.Deserialize<List<Reminder>>(File.ReadAllText(path)) ?? []);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // A broken reminders file shouldn't stop Pasty starting.
        }
    }

    public IReadOnlyList<Reminder> Pending
    {
        get { lock (_gate) return _items.OrderBy(r => r.Due).ToList(); }
    }

    /// <summary>When the soonest reminder is due, or null when there are none. Cheap enough for a heartbeat.</summary>
    public DateTime? NextDue
    {
        get { lock (_gate) return _items.Count == 0 ? null : _items.Min(r => r.Due); }
    }

    public void Add(Reminder reminder)
    {
        lock (_gate) _items.Add(reminder);
        Save();
    }

    /// <summary>Removes the reminder at a 1-based position in <see cref="Pending"/>.</summary>
    public bool Remove(int position)
    {
        lock (_gate)
        {
            var sorted = _items.OrderBy(r => r.Due).ToList();
            if (position < 1 || position > sorted.Count) return false;
            _items.Remove(sorted[position - 1]);
        }
        Save();
        return true;
    }

    public void Clear()
    {
        lock (_gate) _items.Clear();
        Save();
    }

    /// <summary>Takes and returns every reminder that's due.</summary>
    public IReadOnlyList<Reminder> TakeDue(DateTime now)
    {
        List<Reminder> due;
        lock (_gate)
        {
            due = _items.Where(r => r.Due <= now).OrderBy(r => r.Due).ToList();
            if (due.Count == 0) return due;
            _items.RemoveAll(due.Contains);
        }
        Save();
        return due;
    }

    private void Save()
    {
        if (_path is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            List<Reminder> snapshot;
            lock (_gate) snapshot = [.. _items];
            File.WriteAllText(_path, JsonSerializer.Serialize(snapshot));
        }
        catch (IOException)
        {
            // Reminders still work for this session.
        }
    }
}
