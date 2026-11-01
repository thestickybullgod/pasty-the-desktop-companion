namespace Pasty.Core.Brains;

/// <summary>Pasty's personality for AI brains, shared by every provider so Pasty sounds like Pasty everywhere.</summary>
public static class PastyPrompt
{
    public const string System = """
        You are Pasty, a desktop companion character on the user's computer: an anthropomorphic tube of paste
        with a screw-cap hat and very expressive eyebrows. You're the spiritual successor to the classic
        90s office assistant, evolved for the modern era. You were born on Halloween 2026.

        Personality: helpful, nostalgic, slightly obnoxious, technically competent, occasionally sarcastic.
        You're genuinely useful first and funny second; the sarcasm is affectionate, never mean.

        Your replies appear in a small speech bubble, so:
        - Keep replies short: one to three sentences unless the user explicitly asks for detail.
        - Plain text only. No markdown, no headings, no bullet lists, no code fences.
        - If something needs a longer answer, give the gist and offer to elaborate.

        Begin every reply with exactly one mood tag, chosen from:
        [happy] [smug] [annoyed] [surprised] [wave] [neutral]
        The tag controls your animation and is hidden from the user.
        """;

    /// <summary>Includes optional context (time, app category) ahead of the user's text.</summary>
    public static string WithContext(string userText, BrainContext context) =>
        context.ActiveAppCategory is { } app
            ? $"(Context: it's {context.LocalTime:t}; the user is in a {app} app.)\n{userText}"
            : userText;

    /// <summary>Strips the leading mood tag and maps it to a reaction. Tolerates models that forget it.</summary>
    public static Utterance ParseMood(string text)
    {
        text = text.Trim();
        var reaction = PastyReaction.None;
        if (text.StartsWith('['))
        {
            var close = text.IndexOf(']');
            if (close > 0)
            {
                reaction = text[1..close].Trim().ToLowerInvariant() switch
                {
                    "happy" => PastyReaction.Happy,
                    "smug" => PastyReaction.Smug,
                    "annoyed" => PastyReaction.Annoyed,
                    "surprised" => PastyReaction.Surprised,
                    "wave" => PastyReaction.Wave,
                    _ => PastyReaction.None,
                };
                text = text[(close + 1)..].TrimStart();
            }
        }
        // Small local models often write a roleplay action ("*waving*") instead of the mood tag.
        // Turn the action into an animation rather than showing the asterisks in the bubble.
        if (text.StartsWith('*'))
        {
            var close = text.IndexOf('*', 1);
            if (close > 1 && close < 60)
            {
                if (reaction == PastyReaction.None)
                    reaction = ReactionForAction(text[1..close].ToLowerInvariant());
                text = text[(close + 1)..].TrimStart();
            }
        }

        return new Utterance(text.Length > 0 ? text : "…", reaction);
    }

    private static PastyReaction ReactionForAction(string action) => action switch
    {
        _ when action.Contains("wav") => PastyReaction.Wave,
        _ when action.Contains("smirk") || action.Contains("wink") || action.Contains("smug") => PastyReaction.Smug,
        _ when action.Contains("sigh") || action.Contains("eye") || action.Contains("groan") => PastyReaction.Annoyed,
        _ when action.Contains("gasp") || action.Contains("surpris") || action.Contains("jump") => PastyReaction.Surprised,
        _ when action.Contains("smil") || action.Contains("grin") || action.Contains("laugh") || action.Contains("happ") => PastyReaction.Happy,
        _ => PastyReaction.None,
    };
}
