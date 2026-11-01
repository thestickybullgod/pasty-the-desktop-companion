using Pasty.Core.Settings;

namespace Pasty.Core.Context;

/// <summary>Something that can observe the user's environment. Only runs while its consent flag is on.</summary>
public interface IContextSource : IDisposable
{
    string Id { get; }

    /// <summary>Plain-language description shown in Settings, e.g. "Which app you're using (name only)".</summary>
    string Description { get; }

    /// <summary>The user's consent for this source. Sources never decide this themselves.</summary>
    bool IsConsented(PastySettings settings);

    event Action<ContextSignal>? Signal;

    void Start();
    void Stop();
}
