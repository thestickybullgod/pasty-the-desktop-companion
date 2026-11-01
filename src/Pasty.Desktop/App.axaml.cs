using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pasty.Core.Settings;
using Pasty.Desktop.Platform;

namespace Pasty.Desktop;

public partial class App : Application
{
    private PastyCompanion? _companion;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Pasty lives in the tray; closing a window never quits it.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var screenshots = Array.IndexOf(desktop.Args ?? [], StoreScreenshots.Argument);
            if (screenshots >= 0)
            {
                var directory = desktop.Args!.ElementAtOrDefault(screenshots + 1) ?? "store-screenshots";
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await StoreScreenshots.RenderAllAsync(directory);
                    }
                    finally
                    {
                        desktop.Shutdown();
                    }
                });
                base.OnFrameworkInitializationCompleted();
                return;
            }

            // Pasty has entered the chat.
            var store = new SettingsStore(Path.Combine(SettingsStore.DefaultDirectory, "settings.json"));
            _companion = new PastyCompanion(store, PlatformServices.Create());
                        var atStartup = desktop.Args?.Contains(Program.StartupArgument) == true
                || (OperatingSystem.IsWindows() && WindowsPlatform.LaunchedByStartupTask());
            _companion.Start(launchedAtStartup: atStartup);

            SingleInstance.Summoned += message =>
            {
                if (message == "settings") _companion.OpenSettings();
                else _companion.OpenAskFromOutside();
            };
            if (desktop.Args?.Contains(Program.SettingsArgument) == true)
                _companion.OpenSettings();
            desktop.Exit += (_, e) =>
            {
                Program.LogInfo($"Exiting (code {e.ApplicationExitCode}).");
                _companion.Dispose();
            };
            // An always-on companion should never vanish silently: record who asked it to leave.
            desktop.ShutdownRequested += (_, _) => Program.LogInfo("Shutdown requested:\n" + Environment.StackTrace);
            Avalonia.Controls.Window.WindowClosedEvent.AddClassHandler<Window>((w, _) =>
                Program.LogInfo($"{w.GetType().Name} closed:\n" + Environment.StackTrace));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
