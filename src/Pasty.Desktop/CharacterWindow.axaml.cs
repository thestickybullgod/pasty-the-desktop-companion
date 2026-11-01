using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Pasty.Core.Platform;
using Pasty.Desktop.Platform;

namespace Pasty.Desktop;

/// <summary>
/// The small transparent, always-on-top window Pasty lives in. Handles pointer input and placement
/// only; what Pasty says and does is decided by <see cref="PastyCompanion"/>.
/// </summary>
public partial class CharacterWindow : Window
{
    private const double DragThreshold = 4;
    private const double HeadroomTop = 46;

    private readonly IPlatformServices _platform;
    private readonly ScaleTransform RootScale = new();
    private readonly DispatcherTimer _clickTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly DispatcherTimer _gazeTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

    private PixelPoint? _pressScreen;
    private PixelPoint _pressWindow;
    private bool _dragging;
    private (int X, int Y) _lastCursor;

    public CharacterWindow() : this(PlatformServices.Create())
    {
    }

    public CharacterWindow(IPlatformServices platform)
    {
        _platform = platform;
        InitializeComponent();
        Scaler.LayoutTransform = RootScale;

        // Delay single clicks so a double-click doesn't also count as a poke.
        _clickTimer.Tick += (_, _) =>
        {
            _clickTimer.Stop();
            Poked?.Invoke(this, EventArgs.Empty);
        };
        _gazeTimer.Tick += (_, _) => FollowCursor();

        Avatar.PointerPressed += OnPointerPressed;
        Avatar.PointerMoved += OnPointerMoved;
        Avatar.PointerReleased += OnPointerReleased;

        Opened += (_, _) =>
        {
            if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
                WindowsPlatform.HideFromAltTab(handle.Handle);
        };
        PositionChanged += (_, _) => Moved?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Poked;
    public event EventHandler? AskRequested;
    public event EventHandler? Dropped;
    public event EventHandler? DragStarted;
    public event EventHandler? Moved;
    public event EventHandler? MenuRequested;

    /// <summary>Eye tracking polls the cursor, so it only runs while Pasty is awake and visible.</summary>
    public bool TrackCursor
    {
        get => _gazeTimer.IsEnabled;
        set
        {
            if (value && _platform.GetCursorPosition() is not null) _gazeTimer.Start();
            else _gazeTimer.Stop();
        }
    }

    /// <summary>Where the speech bubble's tail should point, in physical pixels.</summary>
    public PixelPoint BubbleAnchor =>
        this.PointToScreen(new Point(Bounds.Width / 2, (HeadroomTop + 2) * RootScale.ScaleY));

    public PixelRect WorkArea =>
        (Screens.ScreenFromWindow(this) ?? Screens.Primary)?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

    public void ApplyScale(double scale)
    {
        scale = Math.Clamp(scale, 0.6, 2.0);
        if (Math.Abs(RootScale.ScaleX - scale) < 0.001) return;

        // Keep Pasty's feet where they are while the window resizes.
        var oldHeight = Bounds.Height * RenderScaling;
        var oldWidth = Bounds.Width * RenderScaling;
        RootScale.ScaleX = RootScale.ScaleY = scale;
        if (!IsVisible) return;
        Dispatcher.UIThread.Post(() =>
        {
            var dh = Bounds.Height * RenderScaling - oldHeight;
            var dw = Bounds.Width * RenderScaling - oldWidth;
            Position = new PixelPoint(Position.X - (int)(dw / 2), Position.Y - (int)dh);
        }, DispatcherPriority.Background);
    }

    /// <summary>Restore a saved position, or park in the bottom-right corner if it's off every screen.</summary>
    public void PlaceAt(double? left, double? top)
    {
        if (left is { } l && top is { } t)
        {
            var saved = new PixelPoint((int)l, (int)t);
            // Require Pasty's middle to be on some screen.
            var middle = saved + new PixelPoint(80, 120);
            if (Screens.All.Any(s => s.Bounds.Contains(middle)))
            {
                Position = saved;
                return;
            }
        }

        var work = (Screens.Primary ?? Screens.All.FirstOrDefault())?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        var scaling = Screens.Primary?.Scaling ?? 1;
        Position = new PixelPoint(
            work.Right - (int)((164 * RootScale.ScaleX + 24) * scaling),
            work.Bottom - (int)(230 * RootScale.ScaleY * scaling));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            MenuRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }
        if (!point.Properties.IsLeftButtonPressed) return;

        if (e.ClickCount == 2)
        {
            _clickTimer.Stop();
            _pressScreen = null;
            AskRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        _pressScreen = this.PointToScreen(e.GetPosition(this));
        _pressWindow = Position;
        _dragging = false;
        e.Pointer.Capture(Avatar);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressScreen is not { } start) return;

        // Work in screen pixels: moving the window doesn't move the reference frame under us.
        var now = this.PointToScreen(e.GetPosition(this));
        var delta = now - start;
        if (!_dragging)
        {
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold) return;
            _dragging = true;
            _clickTimer.Stop();
            DragStarted?.Invoke(this, EventArgs.Empty);
        }
        Position = _pressWindow + delta;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pressScreen is null) return;
        e.Pointer.Capture(null);
        _pressScreen = null;

        if (_dragging)
        {
            _dragging = false;
            Dropped?.Invoke(this, EventArgs.Empty);
            return;
        }

        _clickTimer.Stop();
        _clickTimer.Start();
    }

    private void FollowCursor()
    {
        if (_platform.GetCursorPosition() is not { } cursor || cursor == _lastCursor) return;
        _lastCursor = cursor;

        // Eyes are about 83 units down the artwork, below the headroom.
        var eyes = this.PointToScreen(new Point(Bounds.Width / 2, (HeadroomTop + 83) * RootScale.ScaleY));
        double dx = cursor.X - eyes.X, dy = cursor.Y - eyes.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 1) return;
        // Close to Pasty: look right at it. Far away: a lazy glance in that direction.
        var strength = Math.Min(1, distance / (300 * RenderScaling)) / distance;
        Avatar.Animator.LookToward(dx * strength, dy * strength);
    }
}
