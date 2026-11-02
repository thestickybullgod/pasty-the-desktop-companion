using System.Text;
using System.Text.RegularExpressions;
using Pasty.Core.Plugins;

namespace Pasty.Core.Reminders;

/// <summary>/remind, /reminders and /forget, plus "remind me in 20 minutes to stretch" in plain words.</summary>
public sealed partial class RemindersPlugin(ReminderBook book, Func<DateTime>? clock = null) : IPastyPlugin
{
    private readonly Func<DateTime> _now = clock ?? (() => DateTime.Now);

    public string Id => "pasty.reminders";

    public string DisplayName => "Reminders";

    public void Initialize(IPastyHost host)
    {
        host.Commands.Register(new("remind", "Set a reminder: /remind 20m stretch, /remind at 3pm call mom", Remind));
        host.Commands.Register(new("reminders", "List pending reminders", _ => List()));
        host.Commands.Register(new("forget", "Cancel a reminder: /forget 2, or /forget all", Forget));
    }

    /// <summary>Handles "remind me in 20 minutes to stretch" typed into the ask box; null if it isn't a reminder request.</summary>
    public Utterance? TryHandleNatural(string text)
    {
        var m = NaturalPattern().Match(text.Trim());
        return m.Success ? Remind(m.Groups["rest"].Value) : null;
    }

    private Utterance Remind(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new Utterance("Remind you of what, and when? Try \"/remind 20m stretch\".", PastyReaction.Smug);

        var now = _now();
        if (!ReminderParser.TryParse(args, now, out var reminder, out var error))
            return new Utterance(error, PastyReaction.Annoyed);

        book.Add(reminder);
        return new Utterance($"Got it. {When(reminder.Due, now)} I'll remind you: {reminder.Text}", PastyReaction.Happy);
    }

    private Utterance List()
    {
        var pending = book.Pending;
        if (pending.Count == 0)
            return new Utterance("No reminders. Living in the moment. Respect.", PastyReaction.Smug);

        var now = _now();
        var sb = new StringBuilder("Coming up:\n");
        for (var i = 0; i < pending.Count; i++)
            sb.Append($"{i + 1}. {pending[i].Due:t}{(pending[i].Due.Date != now.Date ? $" ({pending[i].Due:ddd})" : "")}: {pending[i].Text}\n");
        sb.Append("Cancel one with /forget and its number.");
        return new Utterance(sb.ToString(), PastyReaction.Happy);
    }

    private Utterance Forget(string args)
    {
        var arg = args.Trim();
        if (arg.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            book.Clear();
            return new Utterance("All reminders forgotten. I have no memory of this place.", PastyReaction.Smug);
        }
        if (int.TryParse(arg, out var n) && book.Remove(n))
            return new Utterance($"Reminder {n} cancelled.", PastyReaction.Happy);
        return new Utterance("Which one? Check /reminders for the numbers, then /forget 2 (or /forget all).", PastyReaction.Annoyed);
    }

    private static string When(DateTime due, DateTime now)
    {
        var wait = due - now;
        if (wait < TimeSpan.FromHours(1))
        {
            var minutes = (int)Math.Round(wait.TotalMinutes);
            return minutes <= 1 ? "In a minute or so," : $"In {minutes} minutes,";
        }
        return due.Date == now.Date ? $"At {due:t}," : $"Tomorrow at {due:t},";
    }

    [GeneratedRegex(@"^(?:hey\s+\w+[,!]?\s+)?(?:please\s+)?remind\s+me\s+(?<rest>(?:in|at)\s+.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex NaturalPattern();
}
