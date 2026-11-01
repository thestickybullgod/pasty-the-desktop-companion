using Pasty.Core.Commands;

namespace Pasty.Core.Plugins;

/// <summary>
/// The extension point. A plugin is a class with a parameterless constructor implementing this
/// interface, in a DLL dropped into %APPDATA%\Pasty\plugins (loaded only if the user enables plugins).
/// </summary>
public interface IPastyPlugin
{
    string Id { get; }
    string DisplayName { get; }
    void Initialize(IPastyHost host);
}

/// <summary>What Pasty exposes to plugins. Deliberately small: no settings, no context sources, no secrets.</summary>
public interface IPastyHost
{
    CommandRegistry Commands { get; }

    /// <summary>Make Pasty say something. Safe to call from any thread.</summary>
    void Say(Utterance utterance);
}
