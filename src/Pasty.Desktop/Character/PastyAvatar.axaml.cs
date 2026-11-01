using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Pasty.Core;
using Pasty.Core.Animation;
using Pasty.Core.Seasons;

namespace Pasty.Desktop.Character;

/// <summary>
/// Draws Pasty. All motion comes from <see cref="PastyAnimator"/>; this control just copies each
/// pose onto its transforms. The frame timer runs at whatever rate the animator asks for, and
/// stops entirely while Pasty is hidden.
/// </summary>
public partial class PastyAvatar : UserControl
{
    private readonly DispatcherTimer _frameTimer = new(DispatcherPriority.Render);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastFrame;

    // Transforms are built here rather than in XAML: Avalonia only generates fields for named controls.
    private readonly ScaleTransform ShadowScale = new();
    private readonly ScaleTransform Squash = new();
    private readonly RotateTransform Lean = new();
    private readonly TranslateTransform Lift = new();
    private readonly RotateTransform ArmRotate = new();
    private readonly RotateTransform CapRotate = new();
    private readonly TranslateTransform CapLift = new();
    private readonly ScaleTransform EyeLids = new();
    private readonly TranslateTransform Gaze = new();
    private readonly RotateTransform BrowLeftRotate = new();
    private readonly TranslateTransform BrowLeftLift = new();
    private readonly RotateTransform BrowRightRotate = new();
    private readonly TranslateTransform BrowRightLift = new();
    private readonly ScaleTransform MouthOpenScale = new();
    private readonly TranslateTransform BatMove = new();
    private readonly ScaleTransform BatWings = new();

    public PastyAvatar()
    {
        InitializeComponent();

        ShadowLayer.RenderTransform = ShadowScale;
        Figure.RenderTransform = new TransformGroup { Children = { Squash, Lean, Lift } };
        RightArm.RenderTransform = ArmRotate;
        CapLayer.RenderTransform = new TransformGroup { Children = { CapRotate, CapLift } };
        EyesLayer.RenderTransform = EyeLids;
        Pupils.RenderTransform = Gaze;
        BrowLeft.RenderTransform = new TransformGroup { Children = { BrowLeftRotate, BrowLeftLift } };
        BrowRight.RenderTransform = new TransformGroup { Children = { BrowRightRotate, BrowRightLift } };
        MouthOpen.RenderTransform = MouthOpenScale;
        Bat.RenderTransform = BatMove;
        BatWingsLayer.RenderTransform = BatWings;
        _frameTimer.Tick += (_, _) => Frame();
        ApplyPose(PastyPose.Rest);
    }

    public PastyAnimator Animator { get; } = new();

    public PastyState State => Animator.State;

    private Season _season;

    /// <summary>Seasonal costume. Halloween: witch hat, fangs, orange-and-black label, and a bat friend.</summary>
    public Season Season
    {
        get => _season;
        set
        {
            _season = value;
            var halloween = value == Season.Halloween;
            WitchHat.IsVisible = halloween;
            ClassicCap.IsVisible = !halloween;
            Band.Fill = new SolidColorBrush(Color.Parse(halloween ? "#FF7A1A" : "#FF6B57"));
            BandSwirl.Stroke = new SolidColorBrush(Color.Parse(halloween ? "#1A1410" : "#FFFFFF"));
            Animator.BatEnabled = halloween;
            ApplyPose(Animator.Pose);
        }
    }

    public void SetState(PastyState state)
    {
        Animator.SetState(state);
        Wake();
    }

    public void React(PastyReaction reaction)
    {
        Animator.React(reaction);
        Wake();
    }

    /// <summary>Start or stop animating (stop while hidden: zero CPU when nobody can see Pasty).</summary>
    public bool RunAnimation
    {
        get => _frameTimer.IsEnabled;
        set
        {
            if (value == _frameTimer.IsEnabled) return;
            if (value)
            {
                _lastFrame = _clock.Elapsed;
                Wake();
            }
            else
            {
                _frameTimer.Stop();
            }
        }
    }

    private void Wake()
    {
        _frameTimer.Interval = TimeSpan.FromSeconds(1.0 / Animator.RecommendedFps);
        if (!_frameTimer.IsEnabled) _frameTimer.Start();
    }

    private void Frame()
    {
        var now = _clock.Elapsed;
        var pose = Animator.Update(now - _lastFrame);
        _lastFrame = now;
        ApplyPose(pose);
        _frameTimer.Interval = TimeSpan.FromSeconds(1.0 / Animator.RecommendedFps);
    }

    private void ApplyPose(in PastyPose p)
    {
        Squash.ScaleX = p.SquashX;
        Squash.ScaleY = p.SquashY;
        Lean.Angle = p.Lean;
        Lift.Y = p.BodyY;
        ShadowScale.ScaleX = p.ShadowScale;
        Figure.Opacity = p.Opacity;

        EyeLids.ScaleY = p.EyeLid;
        Gaze.X = p.GazeX;
        Gaze.Y = p.GazeY;

        BrowLeftRotate.Angle = p.BrowLeftAngle;
        BrowLeftLift.Y = p.BrowLeftLift;
        BrowRightRotate.Angle = p.BrowRightAngle;
        BrowRightLift.Y = p.BrowRightLift;

        BlushLeft.Opacity = BlushRight.Opacity = p.Blush;
        MouthSmile.IsVisible = p.Mouth == MouthShape.Smile;
        MouthFlat.IsVisible = p.Mouth == MouthShape.Flat;
        MouthSmirk.IsVisible = p.Mouth == MouthShape.Smirk;
        Fangs.IsVisible = _season == Season.Halloween && p.Mouth == MouthShape.Smile;
        MouthOpen.IsVisible = p.Mouth == MouthShape.Open;
        MouthOpenScale.ScaleY = p.MouthOpen;

        ArmRotate.Angle = p.ArmAngle;
        CapRotate.Angle = p.CapTilt;
        CapLift.Y = p.CapLift;
        Zzz.Opacity = p.Zzz;

        Bat.Opacity = p.Bat;
        BatMove.X = p.BatX;
        BatMove.Y = p.BatY;
        BatWings.ScaleY = 1 - 0.65 * p.BatFlap;
    }

    /// <summary>
    /// The whole character, centred in a <paramref name="width"/>×<paramref name="height"/> image with
    /// <paramref name="padding"/> (fraction of the height) above and below. Used for Store tiles.
    /// </summary>
    public static Bitmap RenderFullBody(int width, int height, double padding = 0.12, Season season = Season.None)
    {
        var avatar = new PastyAvatar { Season = season };
        var art = (Control)avatar.Content!;
        avatar.Content = null;
        // Artwork spans x 8–132 and y 5–183 (cap to shadow); fit that box.
        const double artLeft = 8, artTop = 5, artWidth = 124, artHeight = 178;
        var scale = Math.Min(height * (1 - 2 * padding) / artHeight, width * (1 - 2 * padding) / artWidth);
        var offsetX = (width - artWidth * scale) / 2;
        var offsetY = (height - artHeight * scale) / 2;
        art.RenderTransformOrigin = RelativePoint.TopLeft;
        art.RenderTransform = new TransformGroup
        {
            Children =
            {
                new TranslateTransform(-artLeft, -artTop),
                new ScaleTransform(scale, scale),
                new TranslateTransform(offsetX, offsetY),
            },
        };
        var frame = new Canvas { Width = width, Height = height, ClipToBounds = true, Children = { art } };
        frame.Measure(new Size(width, height));
        frame.Arrange(new Rect(0, 0, width, height));

        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(frame);
        return bitmap;
    }

    /// <summary>A square portrait (cap, eyes, top of the body) for tray and window icons.</summary>
    public static Bitmap RenderPortrait(int size, Season season = Season.None)
    {
        // The witch hat is taller than the cap, so the Halloween portrait frames a little higher.
        var halloween = season == Season.Halloween;
        var scale = size / (halloween ? 124.0 : 104.0);
        // Render the artwork canvas itself: an off-screen UserControl has no template applied and draws nothing.
        var avatar = new PastyAvatar { Season = season };
        var art = (Control)avatar.Content!;
        avatar.Content = null;
        art.RenderTransformOrigin = RelativePoint.TopLeft;
        art.RenderTransform = new TransformGroup
        {
            Children = { new TranslateTransform(halloween ? -8 : -18, halloween ? 16 : -2), new ScaleTransform(scale, scale) },
        };
        var frame = new Canvas { Width = size, Height = size, ClipToBounds = true, Children = { art } };
        frame.Measure(new Size(size, size));
        frame.Arrange(new Rect(0, 0, size, size));

        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));
        bitmap.Render(frame);
        return bitmap;
    }
}
