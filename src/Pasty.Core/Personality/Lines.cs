namespace Pasty.Core.Personality;

/// <summary>
/// A line Pasty can say. Spice 0 is wholesome, 1 is cheeky, 2 is the reason people right-click "Hide".
/// Text may use character placeholders ({name}, {noun}, {kind}…; see CharacterInfo.Format).
/// <paramref name="Only"/> limits a line to one character (squeeze jokes only make sense for Classic Pasty's tube).
/// </summary>
public sealed record Line(string Text, int Spice = 0, PastyReaction Reaction = PastyReaction.None, string? Only = null);

/// <summary>Pasty's offline repertoire. Every line here works with no network, no AI, no anything.</summary>
public static class Lines
{
    public static readonly Line[] FirstRun =
    [
        new("{name} has entered the chat. Hi! I'm {name}, your new desktop companion. Drag me anywhere, double-click me to talk, right-click for options.", 0, PastyReaction.Wave),
    ];

    public static readonly Line[] Morning =
    [
        new("Good morning! I've been here all night. Not in a creepy way.", 0, PastyReaction.Wave),
        new("Morning. Coffee first, then we pretend to be productive.", 1, PastyReaction.Wave),
        new("Rise and shine! Well, rise. Shining is optional.", 0, PastyReaction.Wave),
    ];

    public static readonly Line[] Afternoon =
    [
        new("Afternoon! Ready to get things done? I'll supervise.", 0, PastyReaction.Wave),
        new("Ah, the post-lunch slump. I don't eat, so I wouldn't know. I'm just {kind}.", 1, PastyReaction.Wave),
    ];

    public static readonly Line[] Evening =
    [
        new("Evening! Still at it? Respect.", 0, PastyReaction.Wave),
        new("Good evening. The sun has logged off. You might consider it.", 1, PastyReaction.Wave),
    ];

    public static readonly Line[] LateNight =
    [
        new("It's very late. I'm not saying go to bed. I'm strongly implying it.", 1, PastyReaction.Wave),
        new("Ah, the 2 AM coding session. Bold. Bugs come out at night, you know.", 1, PastyReaction.Smug),
        new("Up late? Me too. I'm always up. I don't have eyelids. Well, I do. They're decorative.", 0, PastyReaction.Wave),
    ];

    public static readonly Line[] Idle =
    [
        new("Did you know? I'm 100% original artwork. No paperclips were bent in my making.", 0),
        new("Fun fact: Ctrl+Shift+V pastes without formatting in a lot of apps. You're welcome.", 0, PastyReaction.Happy),
        new("Win+V opens your clipboard history. I'd know. I am, spiritually, a clipboard.", 0, PastyReaction.Smug),
        new("Win+Shift+S takes a screenshot of part of your screen. I recommend my good side.", 0, PastyReaction.Smug),
        new("Win+. opens the emoji picker. Use this power responsibly.", 0),
        new("Have you tried turning it off and on again? Not me. Something else.", 1),
        new("I could be helping right now. I'm just saying. I'm here. Being available.", 1),
        new("Remember to stretch. I can't. I'm {kind}. Live the dream for me.", 0),
        new("Hydration check. Drink some water. I'll wait.", 0),
        new("My ancestors were wiser than you think. Most of them just had bad timing.", 0),
        new("It looks like you're using a computer. Would you like help with that?", 1, PastyReaction.Smug),
        new("Win+L locks your PC. Useful for when coworkers walk past and you're reading about me.", 1, PastyReaction.Smug),
        new("Alt+Tab is great. I just wish it would stop at me sometimes.", 1),
        new("I'm not saying I'm the best feature of Windows 11. I'm letting you reach that conclusion yourself.", 2, PastyReaction.Smug),
        new("Ctrl+Z is the closest thing humans have to time travel. Use it wisely.", 0),
        new("Tip: Win+Ctrl+D makes a new virtual desktop. Somewhere to hide from me. It won't work.", 2, PastyReaction.Smug),
        new("Sometimes I just sit here and think about all the text that's passed through clipboards. Beautiful.", 0),
        new("Your desktop looks great today. I'm biased. I'm on it.", 0, PastyReaction.Happy),
        new("I've been told I'm 'a lot.' I prefer 'abundant.'", 2, PastyReaction.Smug),
        new("Psst. Win+Arrow keys snap windows. I live for this stuff.", 0),
    ];

    public static readonly Line[] Poke =
    [
        new("Hey!", 0, PastyReaction.Poked),
        new("That tickles.", 0, PastyReaction.Poked),
        new("Yes? Can I help you?", 0, PastyReaction.Poked),
        new("Boop received.", 0, PastyReaction.Poked),
        new("Careful, I'm under pressure. Literally. I'm a tube.", 1, PastyReaction.Poked, Characters.Characters.ClassicPastyId),
        new("Careful. Twist me up too far and it gets awkward for everyone.", 1, PastyReaction.Poked, Characters.Characters.GlueyId),
        new("Careful, I'm a squeeze bottle. You know exactly what happens next.", 1, PastyReaction.Poked, Characters.Characters.PastyId),
    ];

    public static readonly Line[] PokeAnnoyed =
    [
        new("Okay, you can stop now.", 1, PastyReaction.Annoyed),
        new("I am not a stress ball.", 1, PastyReaction.Annoyed),
        new("Every click squeezes a little more of me out. Think about that.", 2, PastyReaction.Annoyed, Characters.Characters.ClassicPastyId),
        new("Every poke wears a little more of me down. That's how glue sticks work. Think about that.", 2, PastyReaction.Annoyed, Characters.Characters.GlueyId),
        new("Keep squeezing and you'll be peeling dried glue off your desk all week. Your call.", 2, PastyReaction.Annoyed, Characters.Characters.PastyId),
        new("Do you poke all your software like this?", 2, PastyReaction.Annoyed),
        new("I'm filing a bug report. Against you.", 2, PastyReaction.Annoyed),
    ];

    public static readonly Line[] Dropped =
    [
        new("Ooh, nice spot.", 0, PastyReaction.Jump),
        new("New neighborhood. I like it.", 0, PastyReaction.Jump),
        new("Wheee!", 0, PastyReaction.Jump),
        new("I'll allow it.", 1, PastyReaction.Jump),
        new("A bold move. Very corner-of-the-screen of you.", 1, PastyReaction.Jump),
    ];

    public static readonly Line[] WakeUp =
    [
        new("Huh? I'm up! I wasn't sleeping. I was buffering.", 0, PastyReaction.Surprised),
        new("Oh, you're back! I was just resting my eyes.", 0, PastyReaction.Surprised),
        new("Welcome back. I kept your desktop warm.", 0, PastyReaction.Wave),
    ];

    public static readonly Dictionary<string, Line[]> ActiveApp = new()
    {
        ["code"] =
        [
            new("Coding! Want me to watch? I'm great at watching.", 0),
            new("Ooh, code. Remember: it's not a bug if you call it a feature in the release notes.", 1, PastyReaction.Smug),
            new("I see you've opened an editor. Copy-paste from Stack Overflow responsibly.", 1, PastyReaction.Smug),
            new("Fun fact: 90% of programming is copying and pasting. I'm basically your co-founder.", 2, PastyReaction.Smug),
        ],
        ["terminal"] =
        [
            new("A terminal! Feeling powerful today, are we?", 0),
            new("Please don't type anything that starts with 'rm -rf'. I live here.", 1, PastyReaction.Surprised),
            new("Real hackers use the terminal. And also me. Mostly me.", 1, PastyReaction.Smug),
        ],
        ["writing"] =
        [
            new("It looks like you're writing something. Would you like me to judge it?", 1, PastyReaction.Smug),
            new("Writing? Remember: every great novel started as a blank page and some procrastination.", 0),
            new("If you need a synonym for 'good', I recommend '{name}-approved'.", 1, PastyReaction.Smug),
        ],
        ["spreadsheet"] =
        [
            new("Spreadsheets! The real video games.", 0, PastyReaction.Happy),
            new("Ah, Excel. Where dates go to become numbers against their will.", 1),
            new("Tip: Ctrl+; enters today's date in a cell. I know things.", 0),
        ],
        ["slides"] =
        [
            new("Presentation time? Remember: nobody ever said 'I wish there were more bullet points.'", 1),
            new("Pro tip: one idea per slide. {name} per slide is optional but encouraged.", 0),
        ],
        ["browser"] =
        [
            new("Browsing? Don't forget to come back. I get lonely.", 0),
            new("How many tabs is that now? Don't answer. I know.", 1, PastyReaction.Smug),
            new("Researching, or 'researching'? I won't tell.", 2, PastyReaction.Smug),
        ],
        ["chat"] =
        [
            new("Chatting! Tell them {name} says hi.", 0, PastyReaction.Wave),
            new("Careful: you're typing in a chat app. Every typo is forever.", 1),
        ],
        ["email"] =
        [
            new("Email. Remember: 'per my last email' is the 'with all due respect' of the workplace.", 1, PastyReaction.Smug),
            new("Inbox time? You've got this. Probably.", 0),
        ],
        ["gaming"] =
        [
            new("Gaming? I'll sit quietly. Mostly. I'll go quiet in full-screen.", 0),
            new("Good luck. Have fun. Don't rage-quit on my account.", 1),
        ],
        ["settings"] =
        [
            new("Messing with settings? Look at you, power user.", 0),
            new("If you're looking for the option to uninstall me, it's in a different Settings.", 2, PastyReaction.Smug),
        ],
    };

    public static readonly Dictionary<string, Line[]> Clipboard = new()
    {
        ["url"] =
        [
            new("A link! Paste it somewhere nice.", 0),
            new("Copied a URL. Bold of you to trust the internet.", 1, PastyReaction.Smug),
        ],
        ["email"] =
        [
            new("An email address. Somebody's getting a message.", 0),
        ],
        ["code"] =
        [
            new("That looks like code. Copying from somewhere reputable, I hope.", 1, PastyReaction.Smug),
            new("Code on the clipboard! My people.", 0, PastyReaction.Happy),
        ],
        ["number"] =
        [
            new("A number. I'll try not to think about what it means.", 0),
        ],
        ["huge"] =
        [
            new("Whoa. That's a lot of text. I felt that.", 1, PastyReaction.Surprised),
        ],
        ["text"] =
        [
            new("Copied! I'm practically vibrating.", 0, PastyReaction.Happy),
            new("Copy: done. Paste: pending. The suspense.", 0),
        ],
    };

    public static readonly Line[] OfflineFallback =
    [
        new("Good question. I'm running in offline mode, which is French for 'vibes only'. Try /help for what I can do.", 1),
        new("I'd love to answer that, but my brain is in offline mode. I'm mostly charm right now. Try /help.", 0),
        new("Hmm. I don't know that one without my AI brain. Turn it on in Settings, or try /help.", 0),
        new("That's above my pay grade. I'm not paid. Try /help or enable an AI brain in Settings.", 1, PastyReaction.Smug),
    ];

    /// <summary>Break nudges. {duration} becomes e.g. "an hour" or "90 minutes".</summary>
    public static readonly Line[] BreakNudges =
    [
        new("You've been at it for {duration}. Stand up, stretch, look at something far away. I'll guard the desktop.", 0, PastyReaction.Wave),
        new("{duration} straight! Impressive. Now go get some water. Doctor's orders. I'm not a doctor. I'm {kind}.", 1, PastyReaction.Happy),
        new("Break time. {duration} of focus deserves five minutes of not-focus.", 0, PastyReaction.Wave),
        new("Your eyes have been staring at pixels for {duration}. Give them a little vacation.", 0),
        new("Still going after {duration}? Respect. Also: stretch. Your spine will thank you.", 1, PastyReaction.Smug),
    ];

    /// <summary>What a reminder sounds like when it's due. {text} is what the user asked to be reminded of.</summary>
    public static readonly Line[] ReminderDue =
    [
        new("⏰ Reminder: {text}", 0, PastyReaction.Surprised),
        new("⏰ You asked me to remind you: {text}", 0, PastyReaction.Surprised),
        new("⏰ {text}! That's your reminder. I'm very reliable.", 1, PastyReaction.Surprised),
    ];

    public static readonly Line[] Jokes =
    [
        new("Why did the developer go broke? Because they used up all their cache."),
        new("I told my computer I needed a break. It said: 'No problem, I'll go to sleep.'"),
        new("Why do programmers prefer dark mode? Because light attracts bugs."),
        new("There are 10 kinds of people: those who understand binary and those who don't."),
        new("Why was the JavaScript developer sad? They didn't know how to 'null' their feelings."),
        new("A SQL query walks into a bar, walks up to two tables and asks: 'Can I join you?'"),
        new("I'd tell you a UDP joke, but you might not get it."),
        new("Why did the tube of paste break up with the stapler? Too clingy. Wait, that's me. I'm the clingy one.", Only: Characters.Characters.ClassicPastyId),
        new("Why does everyone want a glue stick at the party? It holds everything together. Also it never leaves. Hi.", Only: Characters.Characters.GlueyId),
        new("Why did the glue bottle get sent to the principal? It kept sticking around after class. Allegedly.", Only: Characters.Characters.PastyId),
        new("How many Windows users does it take to change a light bulb? Just one, but it needs to restart twice."),
    ];
}
