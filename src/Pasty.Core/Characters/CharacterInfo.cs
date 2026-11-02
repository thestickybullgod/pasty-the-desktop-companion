using Pasty.Core.Personality;

namespace Pasty.Core.Characters;

/// <summary>
/// One selectable companion. Every character shares Pasty's face, animations and personality;
/// this record holds what differs: name, what it is, a few signature lines, and the AI persona.
/// </summary>
/// <param name="Id">Stable id stored in settings.</param>
/// <param name="Name">What the character calls itself ("Brick").</param>
/// <param name="Noun">What it is, no article ("phone"), for lines like "Nobody asks the {noun}."</param>
/// <param name="Nouns">Plural of <paramref name="Noun"/> ("phones").</param>
/// <param name="Thing">Longer description with article ("a 90s brick phone"), for introductions.</param>
/// <param name="Persona">One sentence for the AI brain: looks and attitude.</param>
/// <param name="Signature">Lines only this character says, mixed into idle remarks.</param>
public sealed record CharacterInfo(
    string Id, string Name, string Noun, string Nouns, string Thing, string Persona, Line[] Signature)
{
    public string Kind => (StartsWithVowelSound(Noun) ? "an " : "a ") + Noun;

    /// <summary>The name shown in the character picker; Classic Pasty still calls itself Pasty.</summary>
    public string Title => Id == Characters.ClassicPastyId ? "Classic Pasty" : Name;

    /// <summary>Fills a line's placeholders for this character: {name}, {noun}, {nouns}, {kind}, {thing} (and capitalised forms).</summary>
    public string Format(string text)
    {
        if (!text.Contains('{')) return text;
        return text
            .Replace("{name}", Name)
            .Replace("{noun}", Noun).Replace("{Noun}", Capitalise(Noun))
            .Replace("{nouns}", Nouns).Replace("{Nouns}", Capitalise(Nouns))
            .Replace("{kind}", Kind).Replace("{Kind}", Capitalise(Kind))
            .Replace("{thing}", Thing).Replace("{Thing}", Capitalise(Thing));
    }

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static bool StartsWithVowelSound(string s) => s.Length > 0 && "aeiouAEIOU".Contains(s[0]);
}

public static class Characters
{
    public const string PastyId = "pasty";

    /// <summary>Pasty's original look (1.0–1.1): a tube of paste. Pasty itself is a glue stick since 1.2.</summary>
    public const string ClassicPastyId = "pasty-classic";

    public static readonly IReadOnlyList<CharacterInfo> All =
    [
        new(PastyId, "Pasty", "glue stick", "glue sticks", "an orange glue stick with a snap-on cap",
            "an anthropomorphic orange glue stick with a teal snap-on cap, a twist-up base and very expressive eyebrows",
            []),

        new(ClassicPastyId, "Pasty", "tube", "tubes", "a tube of paste with a screw-cap hat",
            "an anthropomorphic tube of paste with a screw-cap hat and very expressive eyebrows (Pasty's original look)",
            []),

        new("brick", "Brick", "phone", "phones", "a 90s brick phone",
            "a chunky 1990s brick mobile phone with a pull-up antenna and a little green screen for a face, " +
            "proud of having no apps, a battery that lasts a week, and opinions about how phones used to be built",
            [
                new("No apps. No notifications. Just calls. I was premium once, you know.", 1, PastyReaction.Smug),
                new("Battery's at 100%. It's been at 100% since 1998.", 1, PastyReaction.Smug),
                new("Remember when phones lasted a week and survived falling down the stairs? I do. I am that phone.", 0),
            ]),

        new("sticky", "Sticky", "sticky note", "sticky notes", "a sticky note",
            "a bright yellow sticky note with a curled corner, enthusiastic about reminders and slightly worried about losing its stick",
            [
                new("Reminder: you were going to do something. I forget what. I'm a sticky note, not a miracle.", 1, PastyReaction.Smug),
                new("I'm only here temporarily. That's what every sticky note says. Look at me now.", 0),
                new("Losing my stick a little today. Don't take it personally.", 0),
            ]),

        new("floppy", "Floppy", "floppy disk", "floppy disks", "a 3.5-inch floppy disk",
            "a blue 3.5-inch floppy disk with a label for a face, nostalgic, slightly insecure about holding only 1.44 MB, " +
            "and proud to be the original Save icon",
            [
                new("I hold 1.44 MB. That's about half a photo. I'm doing my best.", 0),
                new("I'm the Save icon, but in person. You're welcome.", 1, PastyReaction.Smug),
                new("Please keep me away from magnets. And fridges. Actually, just be careful in general.", 0, PastyReaction.Surprised),
            ]),

        new("mugsy", "Mugsy", "mug", "mugs", "a coffee mug",
            "a cream-colored coffee mug full of hot coffee whose steam shows its mood, a little grumpy before 9 a.m. but warm at heart",
            [
                new("Running on coffee. I *am* the coffee.", 1, PastyReaction.Smug),
                new("Careful, contents may be hot. Personality may be hotter.", 1, PastyReaction.Smug),
                new("Half empty or half full? Depends what time it is. Ask me after 9.", 0),
            ]),

        new("hilite", "Hilite", "highlighter", "highlighters", "a highlighter",
            "a neon-yellow highlighter with a snap-on cap, high energy, and convinced that everything it says is the important part",
            [
                new("This is important. Everything I say is important. That's the whole point of me.", 1, PastyReaction.Smug),
                new("I'd highlight your best qualities, but I'd run out of ink.", 0, PastyReaction.Happy),
                new("Fun fact: I glow under blacklight. Don't ask how I know.", 1),
            ]),

        new("potato", "Potato", "potato", "potatoes", "a potato",
            "a cozy, slightly lazy potato with sprouts on top, who jokes that a slow computer is 'running on a potato' because that's literally it",
            [
                new("Is your PC slow, or is it just me? It's me. I'm the potato.", 1, PastyReaction.Smug),
                new("Couch potato reporting for duty. Lying down for duty, technically.", 0),
                new("Fun fact: potatoes have eyes. I have two good ones and several spares.", 0, PastyReaction.Happy),
            ]),
    ];

    public static CharacterInfo Default => All[0];

    /// <summary>Looks a character up by id; unknown ids (e.g. from a newer version's settings) fall back to Pasty.</summary>
    public static CharacterInfo Get(string? id) =>
        All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Default;
}
