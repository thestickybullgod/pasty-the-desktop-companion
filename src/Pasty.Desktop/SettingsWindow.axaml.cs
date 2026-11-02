using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Pasty.Brains.OpenAICompatible;
using Pasty.Core.Brains;
using Pasty.Core.Characters;
using Pasty.Core.Personality;
using Pasty.Core.Plugins;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;
using Pasty.Desktop.Character;
using Pasty.Desktop.Services;

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
        ShowCharacterPortrait();
        BuildCharacterPicker();
        Subtitle.Text = halloween ? "Desktop companion · 🎃 Halloween Edition" : "Desktop companion";
        StartupTitle.Text = OperatingSystem.IsWindows() ? "Start with Windows" : "Start when I log in";

        Bind(SleepChoice, _draft.SleepAfterMinutes, v => _draft.SleepAfterMinutes = (int)v,
            new Choice(0, "Never"), new Choice(5, "After 5 minutes"), new Choice(10, "After 10 minutes"),
            new Choice(20, "After 20 minutes"), new Choice(30, "After 30 minutes"), new Choice(60, "After 1 hour"));
        var hours = Enumerable.Range(0, 24)
            .Select(h => new Choice(TimeSpan.FromHours(h), DateTime.Today.AddHours(h).ToString("t"))).ToArray();
        Bind(QuietStartChoice, _draft.QuietHoursStart, v => _draft.QuietHoursStart = (TimeSpan)v, hours);
        Bind(QuietEndChoice, _draft.QuietHoursEnd, v => _draft.QuietHoursEnd = (TimeSpan)v, hours);
        Bind(BreakChoice, _draft.BreakNudgeMinutes, v => _draft.BreakNudgeMinutes = (int)v,
            new Choice(0, "Off"), new Choice(45, "Every 45 minutes"), new Choice(60, "Every hour"),
            new Choice(90, "Every 90 minutes"), new Choice(120, "Every 2 hours"));
        Bind(HotkeyChoice, _draft.AskHotkey, v => _draft.AskHotkey = (string)v,
            [new Choice("", "Off"), .. GlobalHotkey.Presets.Select(p => new Choice(p, p))]);
        HotkeyChoice.IsEnabled = OperatingSystem.IsWindows();
        Bind(MotionChoice, _draft.Motion, v => _draft.Motion = (MotionPreference)v,
            new Choice(MotionPreference.Auto, "Follow system"), new Choice(MotionPreference.Reduced, "Reduced"),
            new Choice(MotionPreference.Full, "Full"));
        Bind(ChattinessChoice, _draft.Chattiness, v => _draft.Chattiness = (Chattiness)v,
            new Choice(Chattiness.Quiet, "Quiet: only when asked"), new Choice(Chattiness.Polite, "Polite"),
            new Choice(Chattiness.Chatty, "Chatty"), new Choice(Chattiness.Relentless, "Relentless (1997 mode)"));
        Bind(SeasonChoice, _draft.Seasonal, v => _draft.Seasonal = (SeasonalMode)v,
            new Choice(SeasonalMode.Auto, "Automatic"), new Choice(SeasonalMode.AlwaysHalloween, "Always Halloween 🎃"),
            new Choice(SeasonalMode.Off, "Off"));
        Bind(BrainChoice, _draft.Brain, v =>
            {
                _draft.Brain = (BrainKind)v;
                LoadProvider();
            },
            BrainProviders.All.Select(p => new Choice(p.Kind, p.DisplayName)).ToArray());

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

        PreviewButton.Click += (_, _) => PreviewLine.Text = "“" + _preview.Character.Format(_preview.IdleRemark().Text) + "”";
        SaveKeyButton.Click += (_, _) => SaveKey();
        ListModelsButton.Click += async (_, _) => await ListModelsAsync();
        ModelBox.TextChanged += (_, _) => UpdateProfile(p => p with { Model = Blank(ModelBox.Text) });
        EndpointBox.TextChanged += (_, _) => UpdateProfile(p => p with { Endpoint = Blank(EndpointBox.Text) });
        ModelPicker.SelectionChanged += (_, _) =>
        {
            if (ModelPicker.SelectedItem is string model) ModelBox.Text = model;
        };
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
        LoadProvider();
        UpdateDependentState();
        _loading = false;
    }

    /// <summary>One tile per character: portrait and name. Choosing one switches the desktop companion right away.</summary>
    private void BuildCharacterPicker()
    {
        var tiles = Characters.All.Select(info =>
        {
            var tile = new StackPanel { Width = 92, Spacing = 4, Margin = new Thickness(2, 4) };
            tile.Children.Add(new Image
            {
                Source = PastyAvatar.RenderPortrait(144, _companion.Season, info.Id),
                Width = 72, Height = 72, HorizontalAlignment = HorizontalAlignment.Center,
            });
            tile.Children.Add(new TextBlock { Text = info.Title, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 13 });
            return new ListBoxItem { Content = tile, Tag = info.Id, Padding = new Thickness(4), CornerRadius = new CornerRadius(6) };
        }).ToList();

        CharacterPicker.ItemsSource = tiles;
        CharacterPicker.SelectedItem = tiles.FirstOrDefault(t => (string)t.Tag! == Characters.Get(_draft.Character).Id);
        CharacterPicker.SelectionChanged += (_, _) =>
        {
            if (CharacterPicker.SelectedItem is not ListBoxItem { Tag: string id } || id == _draft.Character) return;
            _draft.Character = id;
            ShowCharacterPortrait();
            QueueApply();
        };
    }

    private void ShowCharacterPortrait()
    {
        Portrait.Source = PastyAvatar.RenderPortrait(112, _companion.Season, _draft.Character);
        Icon = new WindowIcon(PastyAvatar.RenderPortrait(64, _companion.Season, _draft.Character));
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
        ProviderOptions.IsVisible = _draft.Brain != BrainKind.Offline;
        var running = _companion.Context.Sources.Where(_companion.Context.IsRunning).Select(s => s.Description).ToList();
        AwarenessStatus.Text = running.Count == 0
            ? "Pasty isn't observing anything. It's just vibing."
            : "Pasty currently notices:\n• " + string.Join("\n• ", running);

        HotkeyStatus.Text = !OperatingSystem.IsWindows()
            ? "Global shortcuts are Windows-only for now. Bind \"pasty\" to a key in your desktop's keyboard settings instead."
            : _companion.HotkeyProblem
                ?? (string.IsNullOrEmpty(_draft.AskHotkey) ? "Off." : $"Press {_draft.AskHotkey} from any app to ask {_companion.Character.Name} something.");
        MotionStatus.Text = "Calmer animation: no bobbing, hopping or bats, and messages appear all at once. "
            + (_companion.ReducedMotion ? "Reduced motion is on." : "Full motion is on.");
    }

    private void ShowPage()
    {
        if (Nav.SelectedItem is not NavItem selected) return;
        PageTitle.Text = selected.Title[selected.Title.IndexOf(' ')..].Trim();
        foreach (NavItem item in Nav.ItemsSource!)
            item.Page.IsVisible = item == selected;
        UpdateDependentState();
    }

    private BrainProvider Provider => BrainProviders.Get(_draft.Brain);

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>Fills the provider-specific cards from the draft's profile for the selected brain.</summary>
    private void LoadProvider()
    {
        var provider = Provider;
        var profile = _draft.ProfileFor(provider.Kind);
        var wasLoading = _loading;
        _loading = true;

        ProviderHeader.Text = provider.DisplayName;
        ProviderIcon.Text = provider.Kind == BrainKind.Ollama ? "🏠" : "☁️";
        ProviderDescription.Text = provider.Description;
        ProviderPrivacy.Text = provider.PrivacyNote;

        EndpointCard.IsVisible = provider.EndpointEditable;
        EndpointBox.Text = profile.Endpoint ?? "";
        EndpointBox.PlaceholderText = provider.DefaultEndpoint ?? "https://…/v1";

        ModelBox.Text = profile.Model ?? "";
        ModelBox.PlaceholderText = provider.DefaultModel ?? "model id";
        ModelHelp.Text = provider.DefaultModel is { } model
            ? $"Leave empty for the default ({model}). Model names change often; 'List models' shows what's available to you."
            : "Required. 'List models' shows what the endpoint offers.";
        ListModelsButton.IsVisible = provider.IsOpenAiCompatible;
        ModelPicker.IsVisible = false;
        ModelPicker.ItemsSource = null;

        KeyCard.IsVisible = provider.NeedsApiKey || provider.Kind == BrainKind.Custom;
        ApiKeyBox.Text = "";
        TestResult.Text = "Sends one short message to check everything works.";
        UpdateKeyStatus();

        _loading = wasLoading;
        UpdateDependentState();
    }

    private void UpdateProfile(Func<BrainProfile, BrainProfile> change)
    {
        if (_loading || _draft.Brain == BrainKind.Offline) return;
        _draft.BrainProfiles[_draft.Brain] = change(_draft.ProfileFor(_draft.Brain));
        QueueApply();
    }

    private void UpdateKeyStatus()
    {
        var provider = Provider;
        var secrets = _companion.Platform.Secrets;
        var hasSaved = secrets.Unprotect(_draft.ProfileFor(provider.Kind).ProtectedApiKey) is not null;
        var env = provider.ApiKeyEnvironmentVariable;
        var hasEnv = env is not null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(env));
        KeyStatus.Text = hasSaved ? "A key is saved. " + secrets.Description
            : hasEnv ? $"Using the {env} environment variable."
            : $"{provider.KeyHelp}" + (env is null ? "" : $" Or set {env}.") + " " + secrets.Description;
    }

    private void SaveKey()
    {
        var key = ApiKeyBox.Text?.Trim() ?? "";
        var protectedKey = key.Length == 0 ? null : _companion.Platform.Secrets.Protect(key, _draft.Brain.ToString());
        ApiKeyBox.Text = "";
        UpdateProfile(p => p with { ProtectedApiKey = protectedKey });
        UpdateKeyStatus();
    }

    private async Task ListModelsAsync()
    {
        if (PastyCompanion.CreateBrain(_draft, _companion.Platform) is not OpenAICompatibleBrain brain)
        {
            TestResult.Text = Provider.NeedsApiKey ? "Save an API key first, then list models." : "Set the endpoint (and a model name) first.";
            return;
        }

        ListModelsButton.IsEnabled = false;
        try
        {
            var models = await brain.ListModelsAsync(CancellationToken.None);
            ModelPicker.ItemsSource = models;
            ModelPicker.IsVisible = models.Count > 0;
            TestResult.Text = models.Count > 0 ? $"Found {models.Count} models. Pick one from the list." : "The provider didn't list any models.";
        }
        catch (Exception ex)
        {
            TestResult.Text = "✖ " + ex.Message;
        }
        finally
        {
            ListModelsButton.IsEnabled = true;
            brain.Dispose();
        }
    }

    private async Task TestBrainAsync()
    {
        var brain = PastyCompanion.CreateBrain(_draft, _companion.Platform);
        if (brain is null)
        {
            TestResult.Text = Provider.NeedsApiKey
                ? "No API key found. Save one above first."
                : "Set the endpoint and model first.";
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
