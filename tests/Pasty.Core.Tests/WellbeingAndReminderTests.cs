using Pasty.Core.Animation;
using Pasty.Core.Brains;
using Pasty.Core.Commands;
using Pasty.Core.Personality;
using Pasty.Core.Plugins;
using Pasty.Core.Reminders;
using Pasty.Core.Settings;
using Pasty.Core.Wellbeing;

namespace Pasty.Core.Tests;

public class WellbeingAndReminderTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 14, 0, 0);

    [Theory]
    [InlineData("20m stretch", 20, "stretch")]
    [InlineData("1h30m check the oven", 90, "check the oven")]
    [InlineData("in 20 minutes to stretch", 20, "stretch")]
    [InlineData("in 2 hours call mom", 120, "call mom")]
    public void Parses_relative_reminders(string input, int minutes, string text)
    {
        Assert.True(ReminderParser.TryParse(input, Now, out var reminder, out _));
        Assert.Equal(Now.AddMinutes(minutes), reminder.Due);
        Assert.Equal(text, reminder.Text);
    }

    [Theory]
    [InlineData("at 3pm call mom", 15, 0)]
    [InlineData("at 15:30 standup", 15, 30)]
    public void Parses_clock_times_today(string input, int hour, int minute)
    {
        Assert.True(ReminderParser.TryParse(input, Now, out var reminder, out _));
        Assert.Equal(Now.Date.AddHours(hour).AddMinutes(minute), reminder.Due);
    }

    [Fact]
    public void A_time_already_past_means_tomorrow()
    {
        Assert.True(ReminderParser.TryParse("at 9am water the plants", Now, out var reminder, out _));
        Assert.Equal(Now.Date.AddDays(1).AddHours(9), reminder.Due);
    }

    [Theory]
    [InlineData("stretch")]                   // no time
    [InlineData("in 300 hours renew passport")] // more than a week away
    [InlineData("at 25:00 nap")]                // not a time
    public void Rejects_reminders_it_cannot_keep(string input)
    {
        Assert.False(ReminderParser.TryParse(input, Now, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void A_bare_duration_works_as_a_timer()
    {
        Assert.True(ReminderParser.TryParse("20m", Now, out var reminder, out _));
        Assert.Equal(Now.AddMinutes(20), reminder.Due);
        Assert.False(string.IsNullOrWhiteSpace(reminder.Text));
    }

    [Fact]
    public void Reminder_book_hands_out_due_reminders_once_and_survives_a_restart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pasty-reminders-{Guid.NewGuid():N}.json");
        try
        {
            var book = new ReminderBook(path);
            book.Add(new Reminder(Now.AddMinutes(30), "later"));
            book.Add(new Reminder(Now.AddMinutes(5), "soon"));
            Assert.Equal(Now.AddMinutes(5), book.NextDue);

            var reopened = new ReminderBook(path);
            Assert.Equal(["soon", "later"], reopened.Pending.Select(r => r.Text));

            Assert.Empty(reopened.TakeDue(Now));
            Assert.Equal(["soon"], reopened.TakeDue(Now.AddMinutes(10)).Select(r => r.Text));
            Assert.Empty(reopened.TakeDue(Now.AddMinutes(10)));
            Assert.True(reopened.Remove(1));
            Assert.Null(reopened.NextDue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Natural_language_requests_set_reminders_and_other_text_passes_through()
    {
        var book = new ReminderBook(null);
        var plugin = new RemindersPlugin(book, () => Now);

        Assert.NotNull(plugin.TryHandleNatural("Remind me in 20 minutes to stretch"));
        Assert.Equal(Now.AddMinutes(20), book.NextDue);
        Assert.Null(plugin.TryHandleNatural("what's the capital of France?"));
        Assert.Null(plugin.TryHandleNatural("can you remind me why tabs beat spaces"));
    }

    [Fact]
    public void Reminder_and_break_lines_fill_in_their_details()
    {
        var engine = new PersonalityEngine(new PastySettings(), new Random(2));
        Assert.Contains("water the plants", engine.ReminderDue("water the plants").Text);
        var nudge = engine.BreakNudge(TimeSpan.FromMinutes(61)).Text;
        Assert.Contains("an hour", nudge);
        Assert.DoesNotContain("{", nudge);
    }

    [Theory]
    [InlineData(22, 8, 23, true)]   // wraps past midnight
    [InlineData(22, 8, 3, true)]
    [InlineData(22, 8, 8, false)]   // end is exclusive
    [InlineData(22, 8, 14, false)]
    [InlineData(12, 13, 12, true)]  // same-day window
    public void Quiet_hours_cover_the_chosen_window(int start, int end, int hour, bool quiet)
    {
        var settings = new PastySettings
        {
            QuietHoursEnabled = true,
            QuietHoursStart = TimeSpan.FromHours(start),
            QuietHoursEnd = TimeSpan.FromHours(end),
        };
        Assert.Equal(quiet, QuietHours.IsQuiet(settings, Now.Date.AddHours(hour)));
        settings.QuietHoursEnabled = false;
        Assert.False(QuietHours.IsQuiet(settings, Now.Date.AddHours(hour)));
    }

    [Fact]
    public void Break_nudge_comes_after_a_long_stretch_and_waits_until_shown()
    {
        var tracker = new BreakTracker();
        var t = Now;
        for (var i = 0; i < 59; i++, t = t.AddMinutes(1))
            Assert.False(tracker.IsDue(t, TimeSpan.FromSeconds(10), 60));

        t = t.AddMinutes(1);
        Assert.True(tracker.IsDue(t, TimeSpan.FromSeconds(10), 60));
        Assert.True(tracker.IsDue(t.AddMinutes(1), TimeSpan.FromSeconds(10), 60));   // not shown yet: still due
        tracker.Nudged(t.AddMinutes(1));
        Assert.False(tracker.IsDue(t.AddMinutes(2), TimeSpan.FromSeconds(10), 60));
    }

    [Fact]
    public void Stepping_away_resets_the_break_clock()
    {
        var tracker = new BreakTracker();
        Assert.False(tracker.IsDue(Now, TimeSpan.Zero, 60));
        Assert.False(tracker.IsDue(Now.AddMinutes(50), BreakTracker.BreakLength, 60));   // a 5-minute break
        Assert.False(tracker.IsDue(Now.AddMinutes(70), TimeSpan.Zero, 60));              // the clock restarts here
        Assert.False(tracker.IsDue(Now.AddMinutes(111), TimeSpan.Zero, 60));             // would have been due without the break
        Assert.True(tracker.IsDue(Now.AddMinutes(130), TimeSpan.Zero, 60));
        Assert.False(tracker.IsDue(Now.AddMinutes(200), TimeSpan.Zero, 0));             // off
    }

    [Fact]
    public void Reduced_motion_keeps_the_body_still()
    {
        var animator = new PastyAnimator { ReducedMotion = true, BatEnabled = true };
        animator.React(PastyReaction.Jump);
        for (var i = 0; i < 60; i++)
        {
            var pose = animator.Update(TimeSpan.FromSeconds(1.0 / 15));
            Assert.Equal(0, pose.BodyY);
            Assert.Equal(0, pose.Lean);
        }
        Assert.True(animator.RecommendedFps <= 15);
    }
}
