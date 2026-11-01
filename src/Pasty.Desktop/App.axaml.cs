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

            // Pasty has entered the chat.
            var store = new SettingsStore(Path.Combine(SettingsStore.DefaultDirectory, "settings.json"));
            _companion = new PastyCompanion(store, PlatformServices.Create());
            _companion.Start(launchedAtStartup: desktop.Args?.Contains(Program.StartupArgument) == true);

            SingleInstance.Summoned += message =>
            {
                if (message == "settings") _companion.OpenSettings();
                else _companion.OpenAskFromOutside();
            };
            if (desktop.Args?.Contains(Program.SettingsArgument) == true)
                _companion.OpenSettings();
            desktop.Exit += (_, _) => _companion.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
