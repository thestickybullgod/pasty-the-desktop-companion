namespace Pasty.Core.Context;

public enum ContextKind
{
    ActiveApp,
    Clipboard,
}

/// <summary>
/// A coarse, already-anonymised observation. Sources must reduce raw data
/// (window titles, clipboard text) to a category before raising a signal;
/// nothing downstream of this type ever sees the raw data.
/// </summary>
public sealed record ContextSignal(ContextKind Kind, string Category);
