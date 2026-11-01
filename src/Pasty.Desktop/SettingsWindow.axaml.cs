using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Pasty.Core.Brains;
using Pasty.Core.Personality;
using Pasty.Core.Plugins;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;
using Pasty.Desktop.Character;

namespace Pasty.Desktop;

/// <summary>
/// Windows 11-style settings. Like the OS Settings app, changes apply immediately; there is no OK button.
/// Edits go to a draft copy that is handed to the companion after a short debounce.
/// </summary>
public partial class SettingsWindow : Window
{
    private sealed record NavItem(string Title, Control Page)
    {
        public override string ToString() => Title;
    }

    private sealed record Choice(object Value, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly PastyCompanion _companion;
    private readonly PastySettings _draft;
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly PersonalityEngine _preview;
    private bool _loading = true;

    public SettingsWindow(PastyCompanion companion)
    {
        _companion = companion;
        _draft = companion.Settings.Clone();
        _preview = new PersonalityEngine(_draft);
        InitializeComponent();
        DataContext = _draft;

        var halloween = companion.Season == Season.Halloween;
        Portrait.Source = PastyAvatar.RenderPortrait(112, companion.Season);
        Icon = new WindowIcon(PastyAvatar.RenderPortrait(64, companion.Season));
        Subtitle.Text = halloween ? "Desktop companion · 🎃 Halloween Edition" : "Desktop companion";
        StartupTitle.Text = OperatingSystem.IsWindows() ? "Start with Windows" : "Start when I log in";

        Bind(SleepChoice, _draft.SleepAfterMinutes, v => _draft.SleepAfterMinutes = (int)v,
            new Choice(0, "Never"), new Choice(5, "After 5 minutes"), new Choice(10, "After 10 minutes"),
            new Choice(20, "After 20 minutes"), new Choice(30, "After 30 minutes"), new Choice(60, "After 1 hour"));
        Bind(ChattinessChoice, _draft.Chattiness, v => _draft.Chattiness = (Chattiness)v,
            new Choice(Chattiness.Quiet, "Quiet: only when asked"), new Choice(Chattiness.Polite, "Polite"),
            new Choice(Chattiness.Chatty, "Chatty"), new Choice(Chattiness.Relentless, "Relentless (1997 mode)"));
        Bind(SeasonChoice, _draft.Seasonal, v => _draft.Seasonal = (SeasonalMode)v,
            new Choice(SeasonalMode.Auto, "Automatic"), new Choice(SeasonalMode.AlwaysHalloween, "Always Halloween 🎃"),
            new Choice(SeasonalMode.Off, "Off"));
        Bind(BrainChoice, _draft.Brain, v => _draft.Brain = (BrainKind)v,
            new Choice(BrainKind.Offline, "Offline (built in)"), new Choice(BrainKind.Claude, "Claude (Anthropic)"));

        Nav.ItemsSource = new[]
        {
            new NavItem("⚙️  General", GeneralPage),
            new NavItem("😏  Personality", PersonalityPage),
            new NavItem("🔒  Awareness & privacy", AwarenessPage),
            new NavItem("🧠  Brain", BrainPage),
            new NavItem("🧩  Plugins", PluginsPage),
            new NavItem(halloween ? "🎃  About" : "ℹ️  About", AboutPage),
        };
        Nav.SelectionChanged += (_, _) => ShowPage();
        Nav.SelectedIndex = 0;

        var sources = companion.Context.Sources;
        ActiveAppDescription.Text = sources.First(s => s.Id == "active-app").Description + ".";
        ClipboardDescription.Text = sources.First(s => s.Id == "clipboard").Description + ".";
        ActiveAppToggle.IsEnabled = companion.Platform.SupportsActiveAppAwareness;
        PluginFolder.Text = PluginLoader.DefaultDirectory;
        CommandList.Text = string.Join("\n", companion.Commands.All.Select(c => $"/{c.Name,-13} {c.Help}"))
            + (companion.PluginErrors.Count > 0 ? "\n\nPlugin errors:\n" + string.Join("\n", companion.PluginErrors) : "");
        VersionText.Text = "Version " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "dev")
            + (halloween ? " · Halloween Edition" : "");
        PlatformInfo.Text = companion.Platform.Name
            + (companion.Platform.Limitations.Count > 0 ? "\n\n• " + string.Join("\n• ", companion.Platform.Limitations) : "\nEverything Pasty can do works here.");

        PreviewButton.Click += (_, _) => PreviewLine.Text = "“" + _preview.IdleRemark().Text + "”";
        SaveKeyButton.Click += (_, _) => SaveKey();
        TestButton.Click += async (_, _) => await TestBrainAsync();
        OpenFolderButton.Click += (_, _) => OpenPluginFolder();

        // Any edit anywhere: apply after a short pause.
        AddHandler(ToggleButton.IsCheckedChangedEvent, (_, _) => QueueApply());
        AddHandler(RangeBase.ValueChangedEvent, (_, _) => QueueApply());
        AddHandler(TextBox.TextChangedEvent, (s, e) =>
        {
            if (e.Source != ApiKeyBox) QueueApply();
        });

        _debounce.Tick += (_, _) => Apply();
        Closing += (_, _) =>
        {
            if (_debounce.IsEnabled) Apply();
        };


        UpdateLabels();
        UpdateKeyStatus();
        UpdateDependentState();
        _loading = false;
    }

    private void Bind(ComboBox box, object current, Action<object> set, params Choice[] choices)
    {
        box.ItemsSource = choices;
        box.SelectedItem = choices.FirstOrDefault(c => c.Value.Equals(current)) ?? choices[0];
        box.SelectionChanged += (_, _) =>
        {
            if (box.SelectedItem is Choice c)
            {
                set(c.Value);
                QueueApply();
            }
        };
    }

    private void QueueApply()
    {
        if (_loading) return;
        UpdateLabels();
        _debounce.Stop();
        _debounce.Start();
    }

    private void Apply()
    {
        _debounce.Stop();
        _companion.ApplySettings(_draft.Clone());
        UpdateDependentState();
    }

    private void UpdateLabels()
    {
        ScaleLabel.Text = $"{_draft.Scale:P0}";
        SarcasmLabel.Text = _preview.SpiceCap switch
        {
            0 => "Saintly",
            1 => "Cheeky",
            _ => "Insufferable",
        };
    }

    private void UpdateDependentState()
    {
        ClaudeOptions.IsVisible = _draft.Brain == BrainKind.Claude;
        var running = _companion.Context.Sources.Where(_companion.Context.IsRunning).Select(s => s.Description).ToList();
        AwarenessStatus.Text = running.Count == 0
            ? "Pasty isn't observing anything. It's just vibing."
            : "Pasty currently notices:\n• " + string.Join("\n• ", running);
    }

    private void ShowPage()
    {
        if (Nav.SelectedItem is not NavItem selected) return;
        PageTitle.Text = selected.Title[selected.Title.IndexOf(' ')..].Trim();
        foreach (NavItem item in Nav.ItemsSource!)
            item.Page.IsVisible = item == selected;
        UpdateDependentState();
    }

    private void UpdateKeyStatus()
    {
        var secrets = _companion.Platform.Secrets;
        var hasSaved = secrets.Unprotect(_draft.ProtectedApiKey) is not null;
        var hasEnv = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        KeyStatus.Text = hasSaved ? "A key is saved. " + secrets.Description
            : hasEnv ? "Using the ANTHROPIC_API_KEY environment variable."
            : "No key yet. Paste one from console.anthropic.com, or set ANTHROPIC_API_KEY. " + secrets.Description;
    }

    private void SaveKey()
    {
        var key = ApiKeyBox.Text?.Trim() ?? "";
        _draft.ProtectedApiKey = key.Length == 0 ? null : _companion.Platform.Secrets.Protect(key);
        ApiKeyBox.Text = "";
        UpdateKeyStatus();
        QueueApply();
    }

    private async Task TestBrainAsync()
    {
        var brain = PastyCompanion.CreateBrain(_draft, _companion.Platform);
        if (brain is null)
        {
            TestResult.Text = "No API key found. Save one above first.";
            return;
        }

        TestButton.IsEnabled = false;
        TestResult.Text = "Asking…";
        try
        {
            var reply = await brain.ReplyAsync("This is a connection test. Say hi in one short sentence.",
                new BrainContext(DateTime.Now), CancellationToken.None);
            TestResult.Text = "✔ " + reply.Text;
        }
        catch (Exception ex)
        {
            TestResult.Text = "✖ " + ex.Message;
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private static void OpenPluginFolder()
    {
        Directory.CreateDirectory(PluginLoader.DefaultDirectory);
        var opener = OperatingSystem.IsWindows() ? "explorer.exe" : "xdg-open";
        try
        {
            Process.Start(new ProcessStartInfo(opener, $"\"{PluginLoader.DefaultDirectory}\"") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // No file manager registered. The path is shown on the card anyway.
        }
    }
}
