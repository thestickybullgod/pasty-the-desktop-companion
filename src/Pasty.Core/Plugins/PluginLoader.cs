using System.Reflection;
using System.Runtime.Loader;

namespace Pasty.Core.Plugins;

public sealed record PluginLoadResult(IReadOnlyList<IPastyPlugin> Loaded, IReadOnlyList<string> Errors);

/// <summary>Discovers <see cref="IPastyPlugin"/> implementations in DLLs from a folder.</summary>
public static class PluginLoader
{
    public static string DefaultDirectory => Path.Combine(Settings.SettingsStore.DefaultDirectory, "plugins");

    public static PluginLoadResult LoadFrom(string directory)
    {
        var loaded = new List<IPastyPlugin>();
        var errors = new List<string>();
        if (!Directory.Exists(directory))
            return new(loaded, errors);

        foreach (var dll in Directory.EnumerateFiles(directory, "*.dll"))
        {
            try
            {
                var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dll), isCollectible: false);
                // Share Pasty.Core with the host so the interface types match.
                context.Resolving += (_, name) =>
                    name.Name == typeof(IPastyPlugin).Assembly.GetName().Name ? typeof(IPastyPlugin).Assembly : null;
                var assembly = context.LoadFromAssemblyPath(dll);
                loaded.AddRange(CreatePlugins(assembly));
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(dll)}: {ex.Message}");
            }
        }
        return new(loaded, errors);
    }

    public static IEnumerable<IPastyPlugin> CreatePlugins(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => typeof(IPastyPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (IPastyPlugin)Activator.CreateInstance(t)!);
}
