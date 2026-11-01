using Pasty.Core.Personality;

namespace Pasty.Core.Seasons;

/// <summary>
/// The Halloween Edition repertoire. Spooky, never scary: puns, costumes and candy.
/// No threats, no gore, nothing that pretends something bad is happening to your PC.
/// </summary>
public static class HalloweenLines
{
    /// <summary>October 31: Halloween, and Pasty's birthday (born on Halloween 2026).</summary>
    public static readonly Line[] HalloweenNight =
    [
        new("Happy Halloween! 🎃 It's also my birthday. I was born on Halloween. Explains a lot, honestly.", 0, PastyReaction.Wave),
        new("It's Halloween AND my birthday! Pasty has entered the crypt. I mean chat. Mostly chat.", 0, PastyReaction.Wave),
    ];

    /// <summary>Said once, at the stroke of midnight between October 31 and November 1.</summary>
    public static readonly Utterance Midnight =
        new("Halloween is over. I refuse to remove the hat.", PastyReaction.Smug);

    /// <summary>November 1: still in costume.</summary>
    public static readonly Line[] DayAfter =
    [
        new("Halloween is over. I refuse to remove the hat.", 0, PastyReaction.Smug),
        new("Yes, I'm still wearing the hat. No, I won't be taking questions.", 1, PastyReaction.Smug),
    ];

    /// <summary>November 2: the hat comes off, under protest.</summary>
    public static readonly Line[] HatRemoved =
    [
        new("They took the hat. I want you to know I did not go quietly.", 1, PastyReaction.Annoyed),
        new("Fine. The hat is off. I'll be in mourning until next October.", 0, PastyReaction.Annoyed),
    ];

    /// <summary>/graveyard: an affectionate visit to retired desktop assistants. Respect, not mockery.</summary>
    public static readonly Line[] Graveyard =
    [
        new("🪦 Here lies the Paperclip, 1997–2007. It saw you writing a letter. It always saw you writing a letter. Rest easy, legend.", 0, PastyReaction.Smug),
        new("🪦 Here lies Rover, the search dog. Fetched files faithfully. A very good boy.", 0, PastyReaction.Happy),
        new("🪦 Here lies Bob, 1995–1996. Gone too soon. Took the whole house with him.", 0),
        new("🪦 Here lies a wizard who once lived in Microsoft Agent. Still out there somewhere, casting 'Please Wait'.", 0),
        new("🪦 Here lies a certain purple gorilla. We don't talk about the purple gorilla.", 1, PastyReaction.Smug),
        new("🪦 They walked so I could... sit on your desktop and make faces. Pour one out. Not on the keyboard.", 0, PastyReaction.Happy),
    ];

    /// <summary>Saying the paperclip's name during Halloween. The third time, it answers.</summary>
    public static readonly Utterance[] SummonSteps =
    [
        new("Shh. Don't say that name in October.", PastyReaction.Surprised),
        new("I'm serious. Say it three times and... look, I'm just saying, it's Halloween.", PastyReaction.Annoyed),
        new("...he's here. 📎👻 It looks like you're trying to summon a ghost. Would you like help with that? ...Kidding. It's just me. Probably.", PastyReaction.Ghost),
    ];

    public static readonly Line[] Greeting =
    [
        new("Happy haunting season! 🎃 I've got my hat on and I'm feeling extra witchy.", 0, PastyReaction.Wave),
        new("Boo! Sorry. Did that work? I've been practicing.", 0, PastyReaction.Wave),
        new("Spooky season is here. I'm contractually obligated to say 'boo' at least twice a day.", 1, PastyReaction.Wave),
    ];

    public static readonly Line[] Idle =
    [
        new("Why don't ghosts use Windows? Too many panes. 👻", 0, PastyReaction.Smug),
        new("I'm not saying your desktop is haunted, but I've seen icons move. It was you. You moved them.", 1, PastyReaction.Smug),
        new("Fun fact: bats are the only flying mammals. My bat friend is the only flying mammal who works here.", 0),
        new("I tried to be a ghost for Halloween, but I'm already pretty pale. Paste joke. Sorry.", 0, PastyReaction.Happy),
        new("Spooky tip: Win+L locks your PC so no ghosts can use it while you're getting candy.", 0),
        new("What's a ghost's favorite key? Esc-ape. 👻", 0, PastyReaction.Smug),
        new("This hat cost me a fortune. And by fortune I mean nothing. I'm a tube.", 1),
        new("Remember: the scariest thing on your PC is a file named final_FINAL_v2_reallyfinal.docx.", 1, PastyReaction.Smug),
        new("I carved a pumpkin. Well, I watched you not carve a pumpkin. Same energy.", 1),
        new("Candy break? I'll guard the desktop. Nothing gets past me. Except everything.", 0),
        new("Skeletons never fight each other. They don't have the guts. 💀", 0, PastyReaction.Smug),
        new("If you hear rattling in your PC, that's just the fans. Probably. Clean your fans.", 1),
        new("I've had zero candy and roughly 400 bytes. I feel incredible. I can see sounds.", 2, PastyReaction.Smug),
        new("The hat speaks to me. It says 'more hat'. I'm considering a second hat.", 2, PastyReaction.Smug),
        new("I've decided I'm a vampire. I don't drink blood. I drink clipboard. Copy something. Please. I'm so thirsty.", 2, PastyReaction.Smug),
        new("Legend says if you stare at the Recycle Bin long enough, it stares back. I'd know. I've been staring for hours.", 2),
        new("Is it weird that I want to be buried in this hat? Not that I'm planning anything. I'm a tube. We last forever.", 2),
    ];

    public static readonly Line[] Poke =
    [
        new("Boo! Ha. Got you. Did I get you?", 0, PastyReaction.Surprised),
        new("Careful, I'm in costume. This hat is load-bearing.", 0, PastyReaction.Poked),
        new("Trick or treat? That was a trick. Mine. On me.", 1, PastyReaction.Poked),
        new("Eek! A human! Oh wait, that's normal.", 0, PastyReaction.Surprised),
    ];

    public static readonly Line[] Dropped =
    [
        new("Swooped in like a bat. 🦇", 0, PastyReaction.Jump),
        new("A new haunt. I approve.", 0, PastyReaction.Jump),
        new("Wheee! Witches usually fly on brooms, but a mouse works.", 0, PastyReaction.Jump),
    ];

    public static readonly Line[] WakeUp =
    [
        new("Who disturbs my slumber? Oh, it's you. Hi!", 0, PastyReaction.Surprised),
        new("I was resting in my crypt. It's a pencil cup. Don't ask.", 0, PastyReaction.Surprised),
    ];

    public static readonly Line[] Jokes =
    [
        new("What do you call a witch at the beach? A sand-witch. 🥪"),
        new("Why did the vampire read the logs? For the stack traces. They're just so... bloodless."),
        new("Why are ghosts bad at lying? You can see right through them."),
        new("What's a mummy's favorite kind of music? Wrap."),
        new("How does a computer celebrate Halloween? With a byte of candy."),
        new("What's a vampire's favorite programming language? Type-script. They love a strong type. And blood."),
    ];

    /// <summary>/trickortreat outcomes. Tricks are silly; none of them touch anything on your PC.</summary>
    public static readonly Line[] Tricks =
    [
        new("TRICK! I swapped your desktop icons... in my imagination. They're fine. Everything's fine.", 0, PastyReaction.Smug),
        new("TRICK! I'm going to tell you a pun and you're not allowed to groan. Ready? I can't think of one. Gotcha.", 1, PastyReaction.Smug),
        new("TRICK! I replaced every 'the' in your documents with 'teh'. Kidding. I can't even read your documents.", 0, PastyReaction.Smug),
    ];

    public static readonly Line[] Treats =
    [
        new("TREAT! 🍬 Win+Shift+S takes a screenshot of part of your screen. Like candy, but useful.", 0, PastyReaction.Happy),
        new("TREAT! 🍫 Ctrl+Shift+T reopens the browser tab you just closed. You're welcome.", 0, PastyReaction.Happy),
        new("TREAT! 🍭 Win+V shows your clipboard history. A whole bag of copies.", 0, PastyReaction.Happy),
        new("TREAT! 🍬 Ctrl+Backspace deletes a whole word at once. Sweet.", 0, PastyReaction.Happy),
        new("TREAT! 🎃 Win+Ctrl+Left/Right switches virtual desktops. One for work, one for spooky wallpapers.", 0, PastyReaction.Happy),
    ];

    public static readonly Line[] Boo =
    [
        new("BOO! 👻 ...was that scary? Be honest. Actually, don't.", 0, PastyReaction.Ghost),
        new("Boooo! I'm a ghost now. A very see-through, tube-shaped ghost.", 0, PastyReaction.Ghost),
        new("BOO! Okay I scared myself a little.", 0, PastyReaction.Ghost),
        new("Wooooo. I am the ghost of documents unsaved. Press Ctrl+S, mortal. 👻", 1, PastyReaction.Ghost),
    ];

    public static readonly Line[] OffSeasonBoo =
    [
        new("It's not even Halloween. But fine. Boo. 👻", 1, PastyReaction.Smug),
    ];
}
