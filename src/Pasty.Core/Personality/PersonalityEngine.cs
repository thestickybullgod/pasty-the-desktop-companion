using Pasty.Core.Characters;
using Pasty.Core.Context;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;

namespace Pasty.Core.Personality;

/// <summary>
/// Decides what Pasty says on its own initiative. Pure logic: no timers, no UI,
/// so it's testable and cheap enough to run all day.
/// </summary>
public sealed class PersonalityEngine
{
    private readonly Random _random;
    private readonly Func<DateTime> _now;
    private readonly Dictionary<string, ShuffleBag<Line>> _bags = [];
    private readonly Dictionary<string, DateTime> _cooldowns = [];
    private int _spiceCap = -1;
    private string? _bagCharacter;
    private int _recentPokes;
    private DateTime _lastPoke;

    public PersonalityEngine(PastySettings settings, Random? random = null, Func<DateTime>? now = null)
    {
        Settings = settings;
        _random = random ?? Random.Shared;
        _now = now ?? (() => DateTime.Now);
    }

    public PastySettings Settings { get; set; }

    /// <summary>The selected companion; its name and nature fill the lines' placeholders.</summary>
    public CharacterInfo Character => Characters.Characters.Get(Settings.Character);

    /// <summary>How long to wait between unprompted idle remarks, or null to never volunteer one.</summary>
    public TimeSpan? IdleInterval => Settings.Chattiness switch
    {
        Chattiness.Quiet => null,
        Chattiness.Polite => TimeSpan.FromMinutes(25),
        Chattiness.Chatty => TimeSpan.FromMinutes(10),
        _ => TimeSpan.FromMinutes(3),
    };

    /// <summary>Minimum gap between reactions to the same kind of context signal.</summary>
    public TimeSpan ContextCooldown => Settings.Chattiness switch
    {
        Chattiness.Relentless => TimeSpan.FromMinutes(2),
        Chattiness.Chatty => TimeSpan.FromMinutes(10),
        _ => TimeSpan.FromMinutes(30),
    };

    public Utterance Greeting()
    {
        if (!Settings.FirstRunComplete)
            return Say("first-run", Lines.FirstRun);

        var today = _now();
        if (Season == Season.Halloween)
        {
            if (SeasonCalendar.IsHalloweenNight(today)) return Say("halloween-night", HalloweenLines.HalloweenNight);
            if (today is { Month: 11, Day: 1 }) return Say("halloween-after", HalloweenLines.DayAfter);
            return Say("halloween-greeting", HalloweenLines.Greeting);
        }
        if (Settings.Seasonal == SeasonalMode.Auto && today is { Month: 11, Day: 2 })
            return Say("hat-removed", HalloweenLines.HatRemoved);

        if (!Settings.TimeOfDayGreetings)
            return new Utterance("Hi again!", PastyReaction.Wave);

        return _now().Hour switch
        {
            >= 5 and < 12 => Say("morning", Lines.Morning),
            >= 12 and < 18 => Say("afternoon", Lines.Afternoon),
            >= 18 and < 23 => Say("evening", Lines.Evening),
            _ => Say("late", Lines.LateNight),
        };
    }

    /// <summary>The current seasonal costume, from the date and the user's seasonal setting.</summary>
    public Season Season => SeasonCalendar.For(_now(), Settings.Seasonal);

    public Utterance IdleRemark()
    {
        // Now and then, a line only the current character would say.
        var signature = Character.Signature;
        if (signature.Length > 0 && _random.NextDouble() < 0.3)
            return Say("signature-" + Character.Id, signature);
        return Seasonal("idle", Lines.Idle, HalloweenLines.Idle, 0.6);
    }

    public Utterance WakeUp() => Seasonal("wake", Lines.WakeUp, HalloweenLines.WakeUp, 0.6);

    public Utterance Dropped() => Seasonal("dropped", Lines.Dropped, HalloweenLines.Dropped, 0.6);

    public Utterance Joke() => Seasonal("joke", Lines.Jokes, HalloweenLines.Jokes, 0.6);

    public Utterance Boo() => Season == Season.Halloween
        ? Say("boo", HalloweenLines.Boo)
        : Say("boo-off", HalloweenLines.OffSeasonBoo);

    public Utterance Graveyard() => Say("graveyard", HalloweenLines.Graveyard);

    private int _summonCount;
    private DateTime _lastSummon;

    /// <summary>
    /// Halloween easter egg: say the paperclip's name three times and something answers.
    /// Returns null outside Halloween or when the text doesn't mention it.
    /// </summary>
    public Utterance? TryEasterEgg(string userText)
    {
        if (Season != Season.Halloween) return null;
        var text = userText.ToLowerInvariant();
        if (!text.Contains("clippy") && !text.Contains("clippit") && !text.Contains("paperclip") && !text.Contains("paper clip"))
            return null;

        var now = _now();
        if (now - _lastSummon > TimeSpan.FromMinutes(3)) _summonCount = 0;
        _lastSummon = now;
        var step = HalloweenLines.SummonSteps[Math.Min(_summonCount, HalloweenLines.SummonSteps.Length - 1)];
        _summonCount = (_summonCount + 1) % HalloweenLines.SummonSteps.Length;
        return step;
    }

    public Utterance TrickOrTreat() => _random.Next(3) == 0
        ? Say("trick", HalloweenLines.Tricks)
        : Say("treat", HalloweenLines.Treats);

    public Utterance OfflineFallback() => Say("offline", Lines.OfflineFallback);

    /// <summary>A gentle nudge to take a break after <paramref name="active"/> of continuous activity.</summary>
    public Utterance BreakNudge(TimeSpan active)
    {
        var line = Say("break", Lines.BreakNudges);
        return line with { Text = line.Text.Replace("{duration}", Describe(active)) };
    }

    /// <summary>A due reminder, in the character's voice.</summary>
    public Utterance ReminderDue(string text)
    {
        var line = Say("reminder", Lines.ReminderDue);
        return line with { Text = line.Text.Replace("{text}", text) };
    }

    private static string Describe(TimeSpan span)
    {
        var minutes = (int)Math.Round(span.TotalMinutes / 15) * 15;
        return minutes switch
        {
            < 60 => $"{Math.Max(1, (int)span.TotalMinutes)} minutes",
            60 => "an hour",
            _ when minutes % 60 == 0 => $"{minutes / 60} hours",
            _ => $"{minutes} minutes",
        };
    }

    public Utterance Poked()
    {
        var now = _now();
        _recentPokes = now - _lastPoke < TimeSpan.FromSeconds(8) ? _recentPokes + 1 : 1;
        _lastPoke = now;

        return _recentPokes >= 4 && SpiceCap >= 1
            ? Say("poke-annoyed", Lines.PokeAnnoyed)
            : Seasonal("poke", Lines.Poke, HalloweenLines.Poke, 0.5);
    }

    /// <summary>Returns a reaction to a context signal, or null if Pasty should stay quiet.</summary>
    public Utterance? React(ContextSignal signal)
    {
        if (Settings.Chattiness == Chattiness.Quiet)
            return null;

        var table = signal.Kind == ContextKind.ActiveApp ? Lines.ActiveApp : Lines.Clipboard;
        if (!table.TryGetValue(signal.Category, out var lines))
            return null;

        var key = $"{signal.Kind}:{signal.Category}";
        var now = _now();
        if (_cooldowns.TryGetValue(key, out var until) && now < until)
            return null;
        _cooldowns[key] = now + ContextCooldown;

        // Even off cooldown, don't react every single time.
        if (_random.NextDouble() > ReactChance)
            return null;

        return Say(key, lines);
    }

    private double ReactChance => Settings.Chattiness switch
    {
        Chattiness.Relentless => 0.9,
        Chattiness.Chatty => 0.6,
        _ => 0.35,
    };

    /// <summary>Highest spice level allowed by the sarcasm slider.</summary>
    public int SpiceCap => Settings.Sarcasm switch
    {
        < 25 => 0,
        < 70 => 1,
        _ => 2,
    };

    /// <summary>In season, mix seasonal lines in; out of season, only the regular ones.</summary>
    private Utterance Seasonal(string bagKey, Line[] regular, Line[] spooky, double chance) =>
        Season == Season.Halloween && _random.NextDouble() < chance
            ? Say("halloween-" + bagKey, spooky)
            : Say(bagKey, regular);

    private Utterance Say(string bagKey, Line[] lines)
    {
        var character = Character;
        if (_spiceCap != SpiceCap || _bagCharacter != character.Id)
        {
            // Sarcasm or character changed: rebuild every bag against the new filter.
            _bags.Clear();
            _spiceCap = SpiceCap;
            _bagCharacter = character.Id;
        }

        if (!_bags.TryGetValue(bagKey, out var bag))
        {
            var forCharacter = lines.Where(l => l.Only is null || l.Only == character.Id).ToArray();
            if (forCharacter.Length == 0) forCharacter = lines;
            var allowed = forCharacter.Where(l => l.Spice <= _spiceCap).ToArray();
            bag = new ShuffleBag<Line>(allowed.Length > 0 ? allowed : [forCharacter.MinBy(l => l.Spice)!], _random);
            _bags[bagKey] = bag;
        }

        var line = bag.Next();
        return new Utterance(character.Format(line.Text), line.Reaction);
    }
}
