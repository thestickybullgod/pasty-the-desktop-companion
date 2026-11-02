using System.Reflection;
using Pasty.Core.Brains;
using Pasty.Core.Characters;
using Pasty.Core.Personality;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;

namespace Pasty.Core.Tests;

public class CharacterTests
{
    [Fact]
    public void Catalog_has_the_companions_with_pasty_first()
    {
        Assert.Equal(["pasty", "pasty-classic", "gluey", "brick", "sticky", "floppy", "mugsy", "hilite", "coppie", "potato"], Characters.Characters.All.Select(c => c.Id));
        Assert.Equal("pasty", Characters.Characters.Default.Id);
        Assert.Equal(Characters.Characters.All.Count, Characters.Characters.All.Select(c => c.Id).Distinct().Count());
        Assert.All(Characters.Characters.All, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
            Assert.False(string.IsNullOrWhiteSpace(c.Persona));
            Assert.False(string.IsNullOrWhiteSpace(c.Thing));
        });
    }

    [Fact]
    public void Unknown_or_missing_ids_fall_back_to_pasty()
    {
        Assert.Equal("pasty", Characters.Characters.Get("dragon").Id);
        Assert.Equal("pasty", Characters.Characters.Get(null).Id);
        Assert.Equal("brick", Characters.Characters.Get("BRICK").Id);
    }

    [Fact]
    public void Format_fills_every_placeholder()
    {
        var potato = Characters.Characters.Get("potato");
        Assert.Equal("I'm a potato. Potatoes can blush. Nobody asks the potato. Potato says hi.",
            potato.Format("I'm {kind}. {Nouns} can blush. Nobody asks the {noun}. {name} says hi."));
        Assert.Equal("an apple", new CharacterInfo("x", "X", "apple", "apples", "", "", []).Kind);
    }

    public static IEnumerable<object[]> EveryCharacter() => Characters.Characters.All.Select(c => new object[] { c.Id });

    [Theory]
    [MemberData(nameof(EveryCharacter))]
    public void No_line_shows_a_raw_placeholder_for_any_character(string id)
    {
        var character = Characters.Characters.Get(id);
        // {duration} and {text} are filled in when the line is said (break nudges, reminders).
        foreach (var text in AllLineTexts())
            Assert.DoesNotContain('{', character.Format(text).Replace("{duration}", "").Replace("{text}", ""));
    }

    [Fact]
    public void Pasty_only_jokes_are_not_told_by_others()
    {
        var engine = new PersonalityEngine(new PastySettings { Character = "brick", Sarcasm = 100 }, new Random(1), () => new DateTime(2026, 6, 15, 12, 0, 0));
        var pokes = Enumerable.Range(0, 200).Select(_ => engine.Poked().Text).ToList();
        Assert.DoesNotContain(pokes, t => t.Contains("under pressure") || t.Contains("squeezes"));
    }

    [Fact]
    public void Characters_mention_themselves_correctly()
    {
        var engine = new PersonalityEngine(new PastySettings { Character = "floppy", Sarcasm = 100 }, new Random(3), () => new DateTime(2026, 6, 15, 12, 0, 0));
        var idle = Enumerable.Range(0, 300).Select(_ => engine.IdleRemark().Text).ToList();
        Assert.Contains(idle, t => t.Contains("1.44 MB"));             // a Floppy signature line
        Assert.DoesNotContain(idle, t => t.Contains("tube"));           // no Pasty-isms
        Assert.Contains(idle, t => t.Contains("I'm a floppy disk"));    // placeholder filled in
    }

    [Fact]
    public void Switching_character_rebuilds_lines()
    {
        var settings = new PastySettings { Sarcasm = 100 };
        var engine = new PersonalityEngine(settings, new Random(5), () => new DateTime(2026, 6, 15, 12, 0, 0));
        engine.IdleRemark();
        settings.Character = "potato";
        var idle = Enumerable.Range(0, 300).Select(_ => engine.IdleRemark().Text).ToList();
        Assert.Contains(idle, t => t.Contains("potato", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ai_persona_follows_the_character()
    {
        var prompt = PastyPrompt.For(Characters.Characters.Get("mugsy"));
        Assert.StartsWith("You are Mugsy", prompt);
        Assert.Contains("coffee mug", prompt);
        Assert.DoesNotContain("tube of paste", prompt);
        Assert.Contains("mood tag", prompt);   // shared rules still apply
        Assert.StartsWith("You are Pasty", PastyPrompt.System);
    }

    [Fact]
    public void Settings_default_to_pasty_and_round_trip_the_character()
    {
        Assert.Equal("pasty", new PastySettings().Character);
        var path = Path.Combine(Path.GetTempPath(), $"pasty-char-{Guid.NewGuid():N}.json");
        try
        {
            new SettingsStore(path).Save(new PastySettings { Character = "hilite" });
            Assert.Equal("hilite", new SettingsStore(path).Load().Character);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Every line text in Lines, HalloweenLines and the character signatures.</summary>
    private static IEnumerable<string> AllLineTexts()
    {
        foreach (var type in new[] { typeof(Lines), typeof(HalloweenLines) })
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                switch (field.GetValue(null))
                {
                    case Line[] lines:
                        foreach (var l in lines) yield return l.Text;
                        break;
                    case Dictionary<string, Line[]> table:
                        foreach (var l in table.Values.SelectMany(v => v)) yield return l.Text;
                        break;
                    case Utterance u:
                        yield return u.Text;
                        break;
                    case Utterance[] us:
                        foreach (var u in us) yield return u.Text;
                        break;
                }
            }
        }
        foreach (var c in Characters.Characters.All)
            foreach (var l in c.Signature) yield return l.Text;
    }
}
