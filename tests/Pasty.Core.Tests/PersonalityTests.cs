using Pasty.Core.Context;
using Pasty.Core.Personality;
using Pasty.Core.Settings;

namespace Pasty.Core.Tests;

public class PersonalityTests
{
    private static PersonalityEngine Engine(PastySettings settings, DateTime? now = null, int seed = 1)
    {
        var clock = now ?? new DateTime(2026, 6, 15, 10, 0, 0); // outside Halloween season
        return new PersonalityEngine(settings, new Random(seed), () => clock);
    }

    [Fact]
    public void FirstRun_greeting_introduces_pasty()
    {
        var greeting = Engine(new PastySettings()).Greeting();
        Assert.StartsWith("Pasty has entered the chat.", greeting.Text);
        Assert.Equal(PastyReaction.Wave, greeting.Reaction);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(20)]
    [InlineData(2)]
    public void Greeting_matches_time_of_day(int hour)
    {
        var settings = new PastySettings { FirstRunComplete = true, Sarcasm = 100 };
        var greeting = Engine(settings, new DateTime(2026, 6, 15, hour, 0, 0)).Greeting();
        var expected = hour switch
        {
            < 5 => Lines.LateNight,
            < 12 => Lines.Morning,
            < 18 => Lines.Afternoon,
            _ => Lines.Evening,
        };
        Assert.Contains(expected, l => l.Text == greeting.Text);
    }

    [Fact]
    public void Zero_sarcasm_only_uses_wholesome_lines()
    {
        var engine = Engine(new PastySettings { Sarcasm = 0 });
        for (var i = 0; i < 100; i++)
        {
            var text = engine.IdleRemark().Text;
            Assert.Equal(0, Lines.Idle.Single(l => l.Text == text).Spice);
        }
    }

    [Fact]
    public void Idle_remarks_do_not_repeat_until_the_bag_is_exhausted()
    {
        var engine = Engine(new PastySettings { Sarcasm = 100 });
        var seen = new HashSet<string>();
        for (var i = 0; i < Lines.Idle.Length; i++)
            Assert.True(seen.Add(engine.IdleRemark().Text), "repeated a line within one cycle");
    }

    [Fact]
    public void Repeated_pokes_get_annoyed()
    {
        var engine = Engine(new PastySettings { Sarcasm = 60 });
        for (var i = 0; i < 3; i++) engine.Poked();
        Assert.Equal(PastyReaction.Annoyed, engine.Poked().Reaction);
    }

    [Fact]
    public void Quiet_mode_never_volunteers()
    {
        var engine = Engine(new PastySettings { Chattiness = Chattiness.Quiet });
        Assert.Null(engine.IdleInterval);
        Assert.Null(engine.React(new ContextSignal(ContextKind.ActiveApp, AppClassifier.Code)));
    }

    [Fact]
    public void Context_reactions_respect_cooldown()
    {
        var engine = Engine(new PastySettings { Chattiness = Chattiness.Relentless, Sarcasm = 100 });
        var signal = new ContextSignal(ContextKind.ActiveApp, AppClassifier.Code);
        Utterance? first = null;
        for (var i = 0; i < 20 && first is null; i++)
            first = engine.React(signal);
        Assert.NotNull(first);
        // Same clock, same category: still cooling down.
        Assert.Null(engine.React(signal));
    }
}

public class ShuffleBagTests
{
    [Fact]
    public void Never_says_the_same_thing_twice_in_a_row()
    {
        var bag = new ShuffleBag<int>([1, 2, 3], new Random(7));
        var last = bag.Next();
        for (var i = 0; i < 300; i++)
        {
            var next = bag.Next();
            Assert.NotEqual(last, next);
            last = next;
        }
    }
}
