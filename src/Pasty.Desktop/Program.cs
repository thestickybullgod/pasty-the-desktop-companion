using Avalonia;
using Avalonia.Threading;
using Pasty.Core.Settings;

namespace Pasty.Desktop;

internal static class Program
{
    public const string StartupArgument = "--startup";
    public const string SettingsArgument = "--settings";

    [STAThread]
    public static int Main(string[] args)
    {
        var export = Array.IndexOf(args, IconExporter.Argument);
        if (export >= 0)
        {
            BuildAvaloniaApp().SetupWithoutStarting();
            IconExporter.Export(args.ElementAtOrDefault(export + 1) ?? "icons");
            return 0;
        }

        // One Pasty per desktop. Launching again just asks the running one to come say hi.
        if (SingleInstance.TrySummonExisting(args.Contains(SettingsArgument) ? "settings" : "summon"))
            return 0;

        SingleInstance.Listen();
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogError(ex);
            throw;
        }
    }

    // Also used by the Avalonia previewer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .AfterSetup(_ => Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                // A companion that runs all day must not die over one bad frame. Log and carry on.
                LogError(e.Exception);
                e.Handled = true;
            })
            .LogToTrace();

    internal static void LogError(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.DefaultDirectory);
            File.AppendAllText(Path.Combine(SettingsStore.DefaultDirectory, "pasty-errors.log"), $"[{DateTime.Now:O}] {ex}\n\n");
        }
        catch (IOException)
        {
        }
    }
}
