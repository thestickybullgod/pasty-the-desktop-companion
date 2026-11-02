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
        var storeAssets = Array.IndexOf(args, IconExporter.StoreAssetsArgument);
        if (storeAssets >= 0)
        {
            BuildAvaloniaApp().SetupWithoutStarting();
            IconExporter.ExportStoreAssets(args.ElementAtOrDefault(storeAssets + 1) ?? "store-assets");
            return 0;
        }

        // Store screenshots need the full app (theme, templates) but not the companion or single-instance check.
        if (args.Contains(StoreScreenshots.Argument) || args.Contains(StoreScreenshots.PreviewArgument))
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

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
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();

        // Pasty's windows are small, so drawing them on the CPU is cheap, and it skips loading the
        // GPU stack (ANGLE/Direct3D, GL): about 100 MB less memory and less CPU at idle.
        // PASTY_RENDERING=gpu brings the GPU renderer back if a machine ever draws oddly.
        if (Environment.GetEnvironmentVariable("PASTY_RENDERING") != "gpu")
        {
            builder = builder
                .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Software] })
                .With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software] });
        }

        return builder
            .AfterSetup(_ => Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                // A companion that runs all day must not die over one bad frame. Log and carry on.
                LogError(e.Exception);
                e.Handled = true;
            })
            .LogToTrace();
    }

    internal static void LogInfo(string message) => Append("pasty.log", message);

    internal static void LogError(Exception ex) => Append("pasty-errors.log", ex.ToString());

    private static void Append(string file, string message)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.DefaultDirectory);
            File.AppendAllText(Path.Combine(SettingsStore.DefaultDirectory, file), $"[{DateTime.Now:O}] {message}\n\n");
        }
        catch (IOException)
        {
        }
    }
}
