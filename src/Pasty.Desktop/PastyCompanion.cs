using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Pasty.Brains.Claude;
using Pasty.Brains.OpenAICompatible;
using Pasty.Core;
using Pasty.Core.Brains;
using Pasty.Core.Characters;
using Pasty.Core.Commands;
using Pasty.Core.Context;
using Pasty.Core.Personality;
using Pasty.Core.Platform;
using Pasty.Core.Plugins;
using Pasty.Core.Reminders;
using Pasty.Core.Seasons;
using Pasty.Core.Settings;
using Pasty.Core.Wellbeing;
using Pasty.Desktop.Character;
using Pasty.Desktop.Services;

namespace Pasty.Desktop;

/// <summary>
/// The composition root and Pasty's "will": decides when to speak, sleep, hide, listen and
/// dress up. Everything here runs on the UI thread.
/// </summary>
public sealed class PastyCompanion : IPastyHost, IDisposable
{
    /// <summary>Unprompted remarks never come closer together than this, whatever else is going on.</summary>
    private static readonly TimeSpan MinGapBetweenRemarks = TimeSpan.FromSeconds(45);

    private readonly SettingsStore _store;
    private readonly CharacterWindow _window;
    private readonly BubbleWindow _bubble = new();
    private readonly PersonalityEngine _personality;
    private readonly OfflineBrain _offline;
    private readonly PastyConversation _conversation;
    private readonly ContextHub _context = new();
    private readonly ActiveAppSource _activeApp;
    private readonly PresenceMonitor _presence;
    private readonly DispatcherTimer _heartbeat = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly List<string> _pluginErrors = [];
    private readonly ReminderBook _reminders = new(Path.Combine(SettingsStore.DefaultDirectory, "reminders.json"));
    private readonly RemindersPlugin _remindersPlugin;
    private readonly BreakTracker _breaks = new();
    private GlobalHotkey? _hotkey;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;
    private CancellationTokenSource? _pendingAsk;

    private PastySettings _settings;
    private DateTime _lastSpoke = DateTime.Now;
    private DateTime _today = DateTime.Today;
    private Season _season = Season.None;
    private Utterance? _sayWhenVisible;
    private DateTime? _snoozedUntil;
    private bool _hiddenByUser;

    internal PastyCompanion(SettingsStore store, IPlatformServices platform)
    {
        _store = store;
        Platform = platform;
        _settings = store.Load();
        _personality = new PersonalityEngine(_settings);
        _offline = new OfflineBrain(_personality);
        _conversation = new PastyConversation(Commands, _offline);
        _conversation.Interceptors.Add(_personality.TryEasterEgg);
        _remindersPlugin = new RemindersPlugin(_reminders);
        _conversation.Interceptors.Add(_remindersPlugin.TryHandleNatural);   // "remind me in 20 minutes to…"

        _window = new CharacterWindow(platform);
        _bubble.AnchorProvider = () => _window.IsVisible ? (_window.BubbleAnchor, _window.WorkArea) : null;

        _activeApp = new ActiveAppSource(platform);
        _context.Register(_activeApp);
        _context.Register(new ClipboardSource(platform, () => _window.Clipboard));
        _context.Signal += OnContextSignal;

        _presence = new PresenceMonitor(platform);

        WireWindow();
        WirePresence();
        _heartbeat.Tick += (_, _) => Heartbeat();
    }

    public IPlatformServices Platform { get; }

    public CommandRegistry Commands { get; } = new();

    public PastySettings Settings => _settings;

    public ContextHub Context => _context;

    public Season Season => _season;

    public IReadOnlyList<string> PluginErrors => _pluginErrors;

    public void Start(bool launchedAtStartup)
    {
        LoadPlugins();
        RegisterCompanionCommands();

        _window.ApplyScale(_settings.Scale);
        _window.Topmost = _settings.AlwaysOnTop;
        _window.Show();
        _window.PlaceAt(_settings.WindowLeft, _settings.WindowTop);
        _window.Avatar.RunAnimation = true;
        _window.TrackCursor = true;

        _tray = new TrayIcon { ToolTipText = Character.Name };
        _tray.Clicked += (_, _) => Summon();
        _tray.Menu = BuildNativeMenu();
        TrayIcon.SetIcons(Application.Current!, [_tray]);

        _hotkey = new GlobalHotkey(_window);
        _hotkey.Pressed += BeginAsk;

        UpdateLook();
        ApplySettings(_settings, save: false);
        _presence.Start();
        _heartbeat.Start();

        // Starting with Windows/login: give the desktop a moment to settle before saying hello.
        var delay = launchedAtStartup ? TimeSpan.FromSeconds(8) : TimeSpan.FromSeconds(0.8);
        DispatcherTimer.RunOnce(CheckTransparency, delay + TimeSpan.FromSeconds(6));
        DispatcherTimer.RunOnce(() =>
        {
            // Quiet hours: arrive without a word (the first-run welcome still explains the basics).
            if (_settings.FirstRunComplete && QuietHours.IsQuiet(_settings, DateTime.Now)) return;
            Speak(_personality.Greeting(), prompted: true);
            if (!_settings.FirstRunComplete)
            {
                _settings.FirstRunComplete = true;
                _store.Save(_settings);
            }
        }, delay);
    }

    /// <summary>
    /// On X11 a window can only be see-through while a compositor runs. Without one, Avalonia paints
    /// Pasty's window white, so explain the fix instead of leaving a mystery square on the desktop.
    /// </summary>
    private void CheckTransparency()
    {
        var level = _window.ActualTransparencyLevel;
        Program.LogInfo($"Transparency: {level} (renderer: {Environment.GetEnvironmentVariable("PASTY_RENDERING") ?? "software"}, " +
            $"desktop: {Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "?"}, session: {Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "?"})");
        if (!OperatingSystem.IsLinux() || level != WindowTransparencyLevel.None) return;

        var mate = (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "").Contains("MATE", StringComparison.OrdinalIgnoreCase);
        Speak(new Utterance(mate
            ? "I'm stuck in a white box! Turn on compositing (Control Center → Windows → General → \"Enable software compositing window manager\") and I'll be see-through."
            : "I'm stuck in a box! Your desktop has no compositor running, so I can't be see-through. Turn on compositing in your window manager's settings.",
            PastyReaction.Surprised), prompted: true);
    }

    /// <summary>Plugin entry point: say something, from any thread.</summary>
    public void Say(Utterance utterance) =>
        Dispatcher.UIThread.Post(() => Speak(utterance, prompted: false));

    public void ApplySettings(PastySettings settings, bool save = true)
    {
        var brainChanged = settings.Brain != _settings.Brain
            || settings.ProfileFor(settings.Brain) != _settings.ProfileFor(settings.Brain)
            || settings.Character != _settings.Character   // the AI plays the selected character
            || (_conversation.Brain is null) == (settings.Brain != BrainKind.Offline);

        // The settings window edits a copy; don't let it roll back state that changed meanwhile.
        if (!ReferenceEquals(settings, _settings))
        {
            settings.WindowLeft = _settings.WindowLeft;
            settings.WindowTop = _settings.WindowTop;
            settings.FirstRunComplete = _settings.FirstRunComplete;
        }

        _settings = settings;
        _personality.Settings = settings;

        _window.Topmost = settings.AlwaysOnTop;
        _bubble.Topmost = settings.AlwaysOnTop;
        _window.ApplyScale(settings.Scale);
        _presence.AwayAfter = TimeSpan.FromMinutes(Math.Max(0, settings.SleepAfterMinutes));
        _context.Apply(settings);
        UpdateLook();

        ReducedMotion = settings.Motion == MotionPreference.Reduced
            || (settings.Motion == MotionPreference.Auto && Platform.PrefersReducedMotion);
        _window.Avatar.Animator.ReducedMotion = ReducedMotion;
        _bubble.InstantText = ReducedMotion;

        _hotkey?.Set(settings.AskHotkey);
        if (_tray is not null) _tray.Menu = BuildNativeMenu();   // the Ask entry shows the shortcut

        if (brainChanged)
        {
            (_conversation.Brain as IDisposable)?.Dispose();
            _conversation.Brain = CreateBrain(settings, Platform);
            if (_conversation.Brain is OpenAICompatibleBrain { IsRemote: false } local)
                _ = local.WarmUpAsync();
        }

        try
        {
            if (Platform.Startup.IsEnabled() != settings.StartWithWindows)
                Platform.Startup.Set(settings.StartWithWindows);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            // Policy may lock the startup location; the toggle simply won't stick.
        }

        UpdateVisibility();
        if (save) _store.Save(settings);
    }

    /// <summary>
    /// Builds the configured brain, or null when it can't work yet (no key, no model), in which case
    /// Pasty stays on its offline brain.
    /// </summary>
    public static IPastyBrain? CreateBrain(PastySettings settings, IPlatformServices platform)
    {
        var provider = BrainProviders.Get(settings.Brain);
        if (provider.Kind == BrainKind.Offline) return null;

        var profile = settings.ProfileFor(provider.Kind);
        var key = platform.Secrets.Unprotect(profile.ProtectedApiKey)
            ?? (provider.ApiKeyEnvironmentVariable is { } env ? Environment.GetEnvironmentVariable(env) : null);
        var model = string.IsNullOrWhiteSpace(profile.Model) ? provider.DefaultModel : profile.Model.Trim();
        var endpoint = string.IsNullOrWhiteSpace(profile.Endpoint) ? provider.DefaultEndpoint : profile.Endpoint.Trim();

        if (provider.NeedsApiKey && string.IsNullOrWhiteSpace(key)) return null;
        if (string.IsNullOrWhiteSpace(model)) return null;

        var prompt = PastyPrompt.For(Characters.Get(settings.Character));
        if (provider.Kind == BrainKind.Claude)
            return new ClaudeBrain(key!, model, prompt);
        if (endpoint is null || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            return null;
        return new OpenAICompatibleBrain(provider.ShortName, endpoint, model, key, systemPrompt: prompt);
    }

    public void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(this);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>True when animation is toned down (the user's choice, or the OS setting under Auto).</summary>
    public bool ReducedMotion { get; private set; }

    /// <summary>Why the ask shortcut isn't working (taken by another app, not on Windows), or null.</summary>
    public string? HotkeyProblem => _hotkey?.Problem;

    /// <summary>Someone launched Pasty again while it was running: come forward and listen.</summary>
    public void OpenAskFromOutside() => BeginAsk();

    public void Exit()
    {
        SavePosition();
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ---------------------------------------------------------------- speech

    private void Speak(Utterance utterance, bool prompted)
    {
        if (!prompted && !CanVolunteer()) return;
        if (!_window.IsVisible)
        {
            // Hidden (snoozed, full-screen...): keep only the latest important line for later.
            if (prompted) _sayWhenVisible = utterance;
            return;
        }

        if (_window.Avatar.State == PastyState.Sleeping)
            _window.TrackCursor = true;

        _lastSpoke = DateTime.Now;
        // Lines may name the character ("{name} says hi"); fill them in for whoever is on duty.
        _bubble.ShowMessage(Character.Format(utterance.Text));
        _window.Avatar.SetState(PastyState.Talking);
        _window.Avatar.React(utterance.Reaction);
    }

    private bool CanVolunteer() =>
        _window.IsVisible
        && !_presence.IsAway
        && !_bubble.IsAsking
        && _pendingAsk is null
        && _window.Avatar.State is PastyState.Idle
        && DateTime.Now - _lastSpoke >= MinGapBetweenRemarks
        && !QuietHours.IsQuiet(_settings, DateTime.Now);   // replies and reminders still get through

    private void OnContextSignal(ContextSignal signal)
    {
        if (!CanVolunteer()) return;
        if (_personality.React(signal) is { } utterance)
            Speak(utterance, prompted: false);
    }

    private void Heartbeat()
    {
        // Midnight checks: costume changes, and the most important line in the codebase.
        if (DateTime.Today != _today)
        {
            var wasHalloweenNight = SeasonCalendar.IsHalloweenNight(_today);
            _today = DateTime.Today;
            UpdateLook();
            if (wasHalloweenNight && _season == Season.Halloween)
            {
                Speak(HalloweenLines.Midnight, prompted: true);
                return;
            }
        }

        if (_snoozedUntil is { } until && DateTime.Now >= until)
        {
            _snoozedUntil = null;
            UpdateVisibility();
            Speak(new Utterance("I'm back! Did you miss me? Don't answer that.", PastyReaction.Wave), prompted: true);
            return;
        }

        var now = DateTime.Now;
        if (DeliverDueReminders(now)) return;

        // Tracked even when Pasty can't speak, so a break taken during a full-screen game still counts.
        if (_breaks.IsDue(now, Platform.GetIdleTime(), _settings.BreakNudgeMinutes) && CanVolunteer())
        {
            _breaks.Nudged(now);
            Speak(_personality.BreakNudge(_breaks.ActiveFor(now)), prompted: false);
            return;
        }

        if (_personality.IdleInterval is { } interval && DateTime.Now - _lastSpoke >= interval && CanVolunteer())
            Speak(_personality.IdleRemark(), prompted: false);
    }

    /// <summary>
    /// Says any reminders that are due. They come through snooze, Hide and quiet hours (the user asked
    /// for them), but wait while a full-screen app is up or the user is typing a question.
    /// </summary>
    private bool DeliverDueReminders(DateTime now)
    {
        if (_reminders.NextDue is not { } next || now < next) return false;
        if (_bubble.IsAsking || _pendingAsk is not null) return false;
        if (_settings.HideWhenFullScreen && _presence.IsFullScreenActive) return false;

        var due = _reminders.TakeDue(now);
        if (due.Count == 0) return false;
        Summon();
        if (_window.Avatar.State == PastyState.Sleeping) _window.Avatar.SetState(PastyState.Idle);
        Speak(_personality.ReminderDue(string.Join(" · ", due.Select(r => r.Text))), prompted: true);
        return true;
    }

    /// <summary>The companion currently on the desktop.</summary>
    public CharacterInfo Character => _personality.Character;

    /// <summary>Applies the selected character and the seasonal costume to the avatar, bubble and icons.</summary>
    private void UpdateLook()
    {
        var season = _personality.Season;
        var character = Character;
        if (season == _season && character.Id == _window.Avatar.CharacterId && _tray?.Icon is not null) return;
        _season = season;

        _window.Avatar.CharacterId = character.Id;
        _window.Avatar.Season = season;
        _bubble.Spooky = season == Season.Halloween;

        var icon = new WindowIcon(PastyAvatar.RenderPortrait(64, season, character.Id));
        _window.Icon = icon;
        if (_tray is not null)
        {
            _tray.Icon = icon;
            _tray.Menu = BuildNativeMenu();   // menu entries use the character's name
        }
        UpdateVisibility();
    }

    // ---------------------------------------------------------------- asking

    private void BeginAsk()
    {
        Summon();
        _window.Avatar.SetState(PastyState.Listening);
        _bubble.ShowAsk(_conversation.Brain is not null
            ? "What can I help with?"
            : "What can I help with? (Offline mode: I'm better at banter than answers.)");
    }

    private async void OnAsked(object? sender, string text)
    {
        _pendingAsk?.Cancel();
        var cts = _pendingAsk = new CancellationTokenSource();

        _window.Avatar.SetState(PastyState.Thinking);
        _bubble.ShowThinking();

        var appCategory = _settings.ShareContextWithAi ? _activeApp.CurrentCategory : null;
        try
        {
            var reply = await _conversation.AskAsync(text, new BrainContext(DateTime.Now, appCategory), cts.Token);
            if (cts.IsCancellationRequested) return;
            Speak(reply, prompted: true);
        }
        catch (OperationCanceledException)
        {
            // Dismissed while thinking.
        }
        finally
        {
            if (_pendingAsk == cts) _pendingAsk = null;
            cts.Dispose();
        }
    }

    // ---------------------------------------------------------------- presence & visibility

    private void UpdateVisibility()
    {
        var hide = _hiddenByUser
            || _snoozedUntil is not null
            || (_settings.HideWhenFullScreen && _presence.IsFullScreenActive);

        if (hide && _window.IsVisible)
        {
            _bubble.HideBubble();
            _window.TrackCursor = false;
            _window.Avatar.RunAnimation = false;
            _window.Hide();
        }
        else if (!hide && !_window.IsVisible)
        {
            _window.Show();
            _window.Avatar.RunAnimation = true;
            _window.TrackCursor = !_presence.IsAway;
            if (_sayWhenVisible is { } pending)
            {
                _sayWhenVisible = null;
                DispatcherTimer.RunOnce(() => Speak(pending, prompted: true), TimeSpan.FromSeconds(1));
            }
        }

        if (_tray is not null)
            _tray.ToolTipText = _snoozedUntil is { } until ? $"{Character.Name} (snoozing until {until:t})"
                : _hiddenByUser ? $"{Character.Name} (hidden; click to bring back)"
                : Character.Name;
        if (_tray is not null) _tray.Menu = BuildNativeMenu();
    }

    private void Summon()
    {
        _hiddenByUser = false;
        _snoozedUntil = null;
        UpdateVisibility();
    }

    private void Snooze(TimeSpan duration, string goodbye)
    {
        Summon();
        _bubble.ShowMessage(goodbye);
        DispatcherTimer.RunOnce(() =>
        {
            _snoozedUntil = DateTime.Now + duration;
            UpdateVisibility();
        }, TimeSpan.FromSeconds(2.2));
    }

    private void WirePresence()
    {
        _presence.WentAway += () =>
        {
            if (_pendingAsk is not null || _bubble.IsAsking) return;
            _bubble.HideBubble();
            _window.Avatar.SetState(PastyState.Sleeping);
            _window.TrackCursor = false;
        };
        _presence.Returned += away =>
        {
            if (_window.Avatar.State != PastyState.Sleeping) return;
            _window.Avatar.SetState(PastyState.Idle);
            _window.TrackCursor = _window.IsVisible;
            if (away >= TimeSpan.FromMinutes(20))
                Speak(_personality.WakeUp(), prompted: false);
        };
        _presence.FullScreenChanged += UpdateVisibility;
    }

    // ---------------------------------------------------------------- window wiring

    private void WireWindow()
    {
        _window.Poked += (_, _) =>
        {
            if (_window.Avatar.State == PastyState.Sleeping)
            {
                _window.Avatar.SetState(PastyState.Idle);
                _window.TrackCursor = true;
                Speak(_personality.WakeUp(), prompted: true);
                return;
            }
            if (_bubble.IsAsking || _pendingAsk is not null) return;
            Speak(_personality.Poked(), prompted: true);
        };
        _window.AskRequested += (_, _) => BeginAsk();
        _window.DragStarted += (_, _) => _window.Avatar.React(PastyReaction.Surprised);
        _window.Moved += (_, _) => { if (_bubble.IsVisible) _bubble.Reposition(); };
        _window.Dropped += (_, _) =>
        {
            SavePosition();
            if (Random.Shared.NextDouble() < 0.4 && !_bubble.IsAsking)
                Speak(_personality.Dropped(), prompted: true);
            else
                _window.Avatar.React(PastyReaction.Jump);
        };
        _window.MenuRequested += (_, _) => ShowContextMenu();

        _bubble.Asked += OnAsked;
        _bubble.FinishedTyping += (_, _) =>
        {
            if (_window.Avatar.State == PastyState.Talking)
                _window.Avatar.SetState(PastyState.Idle);
        };
        _bubble.Dismissed += (_, _) =>
        {
            _pendingAsk?.Cancel();
            if (_window.Avatar.State != PastyState.Sleeping)
                _window.Avatar.SetState(PastyState.Idle);
        };
    }

    private void SavePosition()
    {
        if (!_window.IsVisible) return;
        _settings.WindowLeft = _window.Position.X;
        _settings.WindowTop = _window.Position.Y;
        _store.Save(_settings);
    }

    // ---------------------------------------------------------------- menus

    private sealed record MenuEntry(string Header, Action? Action = null, MenuEntry[]? Children = null)
    {
        public static readonly MenuEntry Separator = new("-");
    }

    private MenuEntry[] MenuEntries()
    {
        var entries = new List<MenuEntry>
        {
            new($"Ask {Character.Name}…" + (_hotkey?.Active is { } keys ? $" ({keys})" : ""), BeginAsk),
            new("Tell me a joke", () => { Summon(); Speak(_personality.Joke(), prompted: true); }),
        };
        if (_season == Season.Halloween)
            entries.Add(new("Trick or treat 🎃", () => { Summon(); Speak(_personality.TrickOrTreat(), prompted: true); }));

        entries.Add(MenuEntry.Separator);
        entries.Add(new("Snooze", Children:
        [
            new("30 minutes", () => Snooze(TimeSpan.FromMinutes(30), "Fine. Thirty minutes. I'll be counting.")),
            new("1 hour", () => Snooze(TimeSpan.FromHours(1), "An hour of peace. You've earned it.")),
            new("Until tomorrow", () => Snooze(DateTime.Today.AddDays(1).AddHours(8) - DateTime.Now,
                "See you tomorrow! Don't do anything I wouldn't do.")),
        ]));
        entries.Add(_window.IsVisible
            ? new($"Hide {Character.Name}", () => { _hiddenByUser = true; UpdateVisibility(); })
            : new($"Show {Character.Name}", Summon));
        entries.Add(MenuEntry.Separator);
        entries.Add(new("Settings", OpenSettings));
        entries.Add(new("Exit", Exit));
        return entries.ToArray();
    }

    private void ShowContextMenu()
    {
        static IEnumerable<Control> Build(IEnumerable<MenuEntry> entries) => entries.Select(e =>
            e == MenuEntry.Separator
                ? (Control)new Separator()
                : CreateItem(e));

        static MenuItem CreateItem(MenuEntry e)
        {
            var item = new MenuItem { Header = e.Header };
            if (e.Children is { } children)
                item.ItemsSource = Build(children).ToList();
            else if (e.Action is { } action)
                item.Click += (_, _) => action();
            return item;
        }

        var menu = new ContextMenu { ItemsSource = Build(MenuEntries()).ToList() };
        menu.Open(_window.Avatar);
    }

    private NativeMenu BuildNativeMenu()
    {
        static void Fill(NativeMenu menu, IEnumerable<MenuEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e == MenuEntry.Separator)
                {
                    menu.Items.Add(new NativeMenuItemSeparator());
                    continue;
                }
                var item = new NativeMenuItem(e.Header);
                if (e.Children is { } children)
                {
                    item.Menu = new NativeMenu();
                    Fill(item.Menu, children);
                }
                else if (e.Action is { } action)
                {
                    item.Click += (_, _) => action();
                }
                menu.Items.Add(item);
            }
        }

        var menu = new NativeMenu();
        Fill(menu, MenuEntries());
        return menu;
    }

    // ---------------------------------------------------------------- plugins & commands

    private void LoadPlugins()
    {
        var plugins = new List<IPastyPlugin> { new BuiltInCommandsPlugin(_personality), _remindersPlugin };
        if (_settings.EnableExternalPlugins)
        {
            var result = PluginLoader.LoadFrom(PluginLoader.DefaultDirectory);
            plugins.AddRange(result.Loaded);
            _pluginErrors.AddRange(result.Errors);
        }

        foreach (var plugin in plugins)
        {
            try { plugin.Initialize(this); }
            catch (Exception ex) { _pluginErrors.Add($"{plugin.DisplayName}: {ex.Message}"); }
        }
    }

    private void RegisterCompanionCommands()
    {
        Commands.Register(new("settings", "Open my settings", _ =>
        {
            Dispatcher.UIThread.Post(OpenSettings);
            return new Utterance("Opening settings. Be gentle.");
        }));
        Commands.Register(new("reset", "Forget our conversation so far", _ =>
        {
            _conversation.Reset();
            return new Utterance("Done. What were we talking about? Exactly.", PastyReaction.Smug);
        }));
        Commands.Register(new("brain", "Which brain am I using?", _ =>
            new Utterance(_conversation.Brain is { } brain
                ? $"I'm running on my {brain.Name} brain ({brain.Model})." + (brain.IsRemote ? "" : " It lives on your computer, like me.")
                : _settings.Brain == BrainKind.Offline
                    ? "Offline brain. All charm, no cloud."
                    : $"I'm set to use {BrainProviders.Get(_settings.Brain).ShortName}, but it's missing a key or model, so you get offline me. Check Settings → Brain.")));
        Commands.Register(new("birthday", "When was I born?", _ =>
            new Utterance(SeasonCalendar.IsHalloweenNight(DateTime.Now)
                ? "Today! I was born on Halloween 2026. 🎂🎃 Cake would be nice. {Nouns} can't eat cake. Get the cake anyway."
                : "I was born on Halloween 2026. 🎃 Explains the hat.", PastyReaction.Happy)));
    }

    public void Dispose()
    {
        _heartbeat.Stop();
        _hotkey?.Dispose();
        _pendingAsk?.Cancel();
        _presence.Dispose();
        _context.Dispose();
        _tray?.Dispose();
        (Platform as IDisposable)?.Dispose();
    }
}
