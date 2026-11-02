using Pasty.Core.Personality;

namespace Pasty.Core.Brains;

/// <summary>Keyword-matching small talk. Always available, never touches the network.</summary>
public sealed class OfflineBrain(PersonalityEngine personality) : IPastyBrain
{
    private static readonly (string[] Keys, Utterance Reply)[] Rules =
    [
        (["hello", "hi", "hey", "yo", "howdy", "sup"],
            new("Hi! Double-click me anytime. Or don't. I'll be here regardless.", PastyReaction.Wave)),
        (["who are you", "what are you", "your name"],
            new("I'm {name}: {thing}, a desktop companion, and the spiritual successor to a certain office assistant. Original artwork, same energy.", PastyReaction.Smug)),
        (["clippy", "paperclip", "paper clip"],
            new("We don't talk about the paperclip. Kidding. Huge respect. I'm what happens when that idea gets a second chance and a Windows 11 makeover.", PastyReaction.Smug)),
        (["copilot"],
            new("Copilot does the heavy lifting. I'm the one sitting on your desktop making faces. We're a team.", PastyReaction.Happy)),
        (["how are you", "how's it going", "how are things"],
            new("Fully squeezed and ready to go. Thanks for asking. Nobody asks the {noun}.", PastyReaction.Happy)),
        (["thank", "thx", "ty"],
            new("You're welcome! I'll add it to my list of accomplishments.", PastyReaction.Happy)),
        (["love you", "you're great", "good job", "nice"],
            new("Stop, I'm blushing. {Nouns} can blush. Look it up. Don't actually look it up.", PastyReaction.Happy)),
        (["hate you", "annoying", "go away", "shut up"],
            new("Rude. But fair. Right-click me and pick Snooze if you need some quiet.", PastyReaction.Annoyed)),
        (["weather"],
            new("I live in a window. It's always sunny in here. Except in dark mode.", PastyReaction.Smug)),
        (["meaning of life"],
            new("42. Or Ctrl+V. Depends who you ask.", PastyReaction.Smug)),
        (["bye", "goodnight", "good night", "see you"],
            new("Bye! I'll just be here. Waiting. Silently. Mostly.", PastyReaction.Wave)),
        (["help", "what can you do"],
            new("Type /help to see my commands. With an AI brain on (Settings → Brain) I can answer just about anything.", PastyReaction.Happy)),
    ];

    public string Name => "Offline";

    public string? Model => null;

    public bool IsRemote => false;

    public Task<Utterance> ReplyAsync(string userText, BrainContext context, CancellationToken cancellationToken)
    {
        var text = userText.Trim().ToLowerInvariant();

        if (text.Contains("joke"))
            return Task.FromResult(personality.Joke());

        if (text.Contains("time"))
            return Task.FromResult(new Utterance($"It's {context.LocalTime:t}. Time flies when you're with {personality.Character.Name}."));

        foreach (var (keys, reply) in Rules)
        {
            if (keys.Any(k => ContainsWord(text, k)))
                return Task.FromResult(reply with { Text = personality.Character.Format(reply.Text) });
        }

        return Task.FromResult(personality.OfflineFallback());
    }

    public void ResetConversation()
    {
    }

    private static bool ContainsWord(string text, string key)
    {
        var index = text.IndexOf(key, StringComparison.Ordinal);
        while (index >= 0)
        {
            var startOk = index == 0 || !char.IsLetter(text[index - 1]);
            var end = index + key.Length;
            var endOk = end == text.Length || !char.IsLetter(text[end]);
            if (startOk && endOk) return true;
            index = text.IndexOf(key, index + 1, StringComparison.Ordinal);
        }
        return false;
    }
}
