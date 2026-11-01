namespace Pasty.Core.Brains;

/// <summary>
/// Optional context passed to a brain. ActiveAppCategory is only populated when the user has
/// opted in to both app awareness and sharing context with the AI.
/// </summary>
public sealed record BrainContext(DateTime LocalTime, string? ActiveAppCategory = null);

/// <summary>Turns free-form user text into a reply. Implementations own their conversation history.</summary>
public interface IPastyBrain
{
    string Name { get; }

    /// <summary>The model in use, or null for brains without one.</summary>
    string? Model { get; }

    /// <summary>True if this brain needs the network. Pasty falls back to offline when it fails.</summary>
    bool IsRemote { get; }

    Task<Utterance> ReplyAsync(string userText, BrainContext context, CancellationToken cancellationToken);

    void ResetConversation();
}

/// <summary>A brain failure with a message fit for Pasty to say out loud.</summary>
public sealed class BrainException(string pastyMessage, Exception? inner = null) : Exception(pastyMessage, inner);
