using Pasty.Core.Animation;
using Pasty.Core.Personality;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;

namespace Pasty.Core.Tests;

public class HalloweenTests
{
    private static PersonalityEngine Engine(DateTime now, SeasonalMode mode = SeasonalMode.Auto) =>
        new(new PastySettings { FirstRunComplete = true, Seasonal = mode, Sarcasm = 100 }, new Random(5), () => now);

    [Theory]
    [InlineData(10, 14, Season.None)]
    [InlineData(10, 15, Season.Halloween)]
    [InlineData(10, 31, Season.Halloween)]
    [InlineData(11, 1, Season.Halloween)]   // the hat stays on
    [InlineData(11, 2, Season.None)]
    [InlineData(6, 1, Season.None)]
    public void Auto_season_follows_the_calendar(int month, int day, Season expected) =>
        Assert.Equal(expected, SeasonCalendar.For(new DateTime(2026, month, day), SeasonalMode.Auto));

    [Fact]
    public void Season_modes_override_the_calendar()
    {
        Assert.Equal(Season.Halloween, SeasonCalendar.For(new DateTime(2026, 6, 1), SeasonalMode.AlwaysHalloween));
        Assert.Equal(Season.None, SeasonCalendar.For(new DateTime(2026, 10, 31), SeasonalMode.Off));
    }

    [Fact]
    public void Halloween_night_greeting_mentions_the_birthday()
    {
        var greeting = Engine(new DateTime(2026, 10, 31, 20, 0, 0)).Greeting();
        Assert.Contains("birthday", greeting.Text);
    }

    [Fact]
    public void November_first_refuses_to_remove_the_hat()
    {
        var engine = Engine(new DateTime(2026, 11, 1, 9, 0, 0));
        var texts = Enumerable.Range(0, 4).Select(_ => engine.Greeting().Text).ToList();
        Assert.Contains(HalloweenLines.Midnight.Text, texts);
    }

    [Fact]
    public void November_second_takes_the_hat_off_under_protest()
    {
        var greeting = Engine(new DateTime(2026, 11, 2, 9, 0, 0)).Greeting();
        Assert.Contains(HalloweenLines.HatRemoved, l => l.Text == greeting.Text);
    }

    [Fact]
    public void Saying_the_name_three_times_summons_a_ghost()
    {
        var engine = Engine(new DateTime(2026, 10, 31, 23, 0, 0));
        Assert.Null(engine.TryEasterEgg("hello there"));
        engine.TryEasterEgg("clippy");
        engine.TryEasterEgg("Clippy?");
        var third = engine.TryEasterEgg("CLIPPY!");
        Assert.Equal(PastyReaction.Ghost, third?.Reaction);
    }

    [Fact]
    public void Easter_egg_sleeps_outside_halloween()
    {
        Assert.Null(Engine(new DateTime(2026, 3, 1)).TryEasterEgg("clippy"));
    }

    [Fact]
    public void Off_season_boo_is_reluctant()
    {
        Assert.Contains("not even Halloween", Engine(new DateTime(2026, 3, 1)).Boo().Text);
    }

    [Fact]
    public void Halloween_lines_never_pretend_the_pc_is_in_trouble()
    {
        // "Spooky, not harmful": nothing that could read as a real security or system warning.
        string[] alarming = ["virus", "hacked", "deleted your", "formatting", "infected", "malware", "your files are"];
        var all = HalloweenLines.Idle.Concat(HalloweenLines.Greeting).Concat(HalloweenLines.Poke)
            .Concat(HalloweenLines.Tricks).Concat(HalloweenLines.Boo).Concat(HalloweenLines.Graveyard)
            .Select(l => l.Text).Concat(HalloweenLines.SummonSteps.Select(u => u.Text));
        foreach (var text in all)
            Assert.DoesNotContain(alarming, word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}

public class AnimatorTests
{
    private static PastyPose Run(PastyAnimator animator, double seconds)
    {
        var pose = animator.Pose;
        for (var t = 0.0; t < seconds; t += 1 / 30.0)
            pose = animator.Update(TimeSpan.FromSeconds(1 / 30.0));
        return pose;
    }

    [Fact]
    public void Sleeping_closes_the_eyes_and_slows_down()
    {
        var animator = new PastyAnimator(new Random(1));
        animator.SetState(PastyState.Sleeping);
        var pose = Run(animator, 3);
        Assert.True(pose.EyeLid < 0.2);
        Assert.True(animator.RecommendedFps <= 10);
    }

    [Fact]
    public void Reactions_wear_off()
    {
        var animator = new PastyAnimator(new Random(1));
        animator.React(PastyReaction.Ghost);
        var haunted = Run(animator, 1.5);
        Assert.True(haunted.Opacity < 0.7, "ghost should be see-through mid-reaction");
        var after = Run(animator, 4);
        Assert.False(animator.IsReacting);
        Assert.Equal(1, after.Opacity);
    }

    [Fact]
    public void Ghost_never_disappears_completely()
    {
        var animator = new PastyAnimator(new Random(1));
        animator.React(PastyReaction.Ghost);
        for (var i = 0; i < 120; i++)
            Assert.True(animator.Update(TimeSpan.FromSeconds(1 / 30.0)).Opacity >= 0.39);
    }

    [Fact]
    public void Bat_only_flies_when_enabled()
    {
        var animator = new PastyAnimator(new Random(1));
        Assert.Equal(0, Run(animator, 30).Bat);
        animator.BatEnabled = true;
        var maxBat = 0.0;
        for (var i = 0; i < 30 * 20; i++)
            maxBat = Math.Max(maxBat, animator.Update(TimeSpan.FromSeconds(1 / 30.0)).Bat);
        Assert.True(maxBat > 0.5);
    }

    [Fact]
    public void Talking_flaps_the_mouth()
    {
        var animator = new PastyAnimator(new Random(1));
        animator.SetState(PastyState.Talking);
        var openings = Enumerable.Range(0, 30).Select(_ => animator.Update(TimeSpan.FromSeconds(1 / 30.0)).MouthOpen).ToList();
        Assert.All(openings, o => Assert.InRange(o, 0.3, 1.0));
        Assert.True(openings.Max() - openings.Min() > 0.4);
    }

    [Fact]
    public void Smug_raises_one_brow_and_lowers_the_other()
    {
        var animator = new PastyAnimator(new Random(1));
        animator.React(PastyReaction.Smug);
        var pose = Run(animator, 1);
        Assert.True(pose.BrowLeftLift < -7, "the raised brow goes well up");
        Assert.True(pose.BrowRightLift > 0, "the other brow comes down");
    }

    [Fact]
    public void Starting_to_listen_flashes_the_brows()
    {
        var animator = new PastyAnimator(new Random(1));
        Run(animator, 1);
        animator.SetState(PastyState.Listening);
        var highest = Enumerable.Range(0, 20).Min(_ => animator.Update(TimeSpan.FromSeconds(1 / 30.0)).BrowLeftLift);
        var settled = Run(animator, 2).BrowLeftLift;
        Assert.True(highest < settled - 2, "a quick raise above the listening face, then back down");
    }

    [Fact]
    public void Idle_now_and_then_raises_a_single_brow()
    {
        var animator = new PastyAnimator(new Random(3));
        var lopsided = 0.0;
        for (var i = 0; i < 30 * 50; i++)
        {
            var p = animator.Update(TimeSpan.FromSeconds(1 / 30.0));
            lopsided = Math.Max(lopsided, Math.Abs(p.BrowLeftLift - p.BrowRightLift));
        }
        Assert.True(lopsided > 6, "within 50 seconds of idling, one brow goes up on its own");
    }
}
