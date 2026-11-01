using Pasty.Core.Settings;

namespace Pasty.Core.Context;

/// <summary>
/// The single chokepoint for environment awareness. Starts a source only when the
/// user has consented, stops it the moment consent is withdrawn, and forwards signals.
/// </summary>
public sealed class ContextHub : IDisposable
{
    private readonly List<IContextSource> _sources = [];
    private readonly HashSet<IContextSource> _running = [];

    public event Action<ContextSignal>? Signal;

    public IReadOnlyList<IContextSource> Sources => _sources;

    public bool IsRunning(IContextSource source) => _running.Contains(source);

    public void Register(IContextSource source)
    {
        _sources.Add(source);
        source.Signal += Forward;
    }

    public void Apply(PastySettings settings)
    {
        foreach (var source in _sources)
        {
            var consented = source.IsConsented(settings);
            if (consented && _running.Add(source))
                source.Start();
            else if (!consented && _running.Remove(source))
                source.Stop();
        }
    }

    private void Forward(ContextSignal signal) => Signal?.Invoke(signal);

    public void Dispose()
    {
        foreach (var source in _sources)
        {
            source.Signal -= Forward;
            source.Dispose();
        }
        _sources.Clear();
        _running.Clear();
    }
}
