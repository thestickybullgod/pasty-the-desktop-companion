namespace Pasty.Core;

/// <summary>What Pasty is doing right now. Drives the looping body animation.</summary>
public enum PastyState
{
    Idle,
    Listening,
    Thinking,
    Talking,
    Sleeping,
}

/// <summary>One-shot expressions layered on top of the current state.</summary>
public enum PastyReaction
{
    None,
    Happy,
    Smug,
    Annoyed,
    Surprised,
    Poked,
    Jump,
    Wave,
    /// <summary>Halloween Edition: goes translucent and floats for a moment. Spooky, not scary.</summary>
    Ghost,
}

/// <summary>Something Pasty says, plus the face it makes while saying it.</summary>
public sealed record Utterance(string Text, PastyReaction Reaction = PastyReaction.None);
