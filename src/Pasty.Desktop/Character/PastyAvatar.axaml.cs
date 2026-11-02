using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Pasty.Core;
using Pasty.Core.Animation;
using Pasty.Core.Seasons;
using Pasty.Desktop.Character.Bodies;

namespace Pasty.Desktop.Character;

/// <summary>
/// The shared character rig. All motion comes from <see cref="PastyAnimator"/>; this control copies
/// each pose onto its transforms. The look comes from a <see cref="CharacterBody"/> (Pasty, Brick,
/// Potato…) hosted inside the rig. The frame timer runs at whatever rate the animator asks for, and
/// stops entirely while the character is hidden.
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
    // Per-body fit: arms, face, blush and witch hat nudged to suit the body's shape.
    private readonly TranslateTransform LeftArmShift = new();
    private readonly TranslateTransform RightArmShift = new();
    private readonly TranslateTransform FaceShift = new();
    private readonly TranslateTransform BlushLeftShift = new();
    private readonly TranslateTransform BlushRightShift = new();
    private readonly TranslateTransform HatShift = new();

    private CharacterBody? _body;
    private string _characterId = "";
    private Season _season;

    public PastyAvatar()
    {
        InitializeComponent();

        ShadowLayer.RenderTransform = ShadowScale;
        Figure.RenderTransform = new TransformGroup { Children = { Squash, Lean, Lift } };
        LeftArm.RenderTransform = LeftArmShift;
        RightArm.RenderTransform = new TransformGroup { Children = { ArmRotate, RightArmShift } };
        CapLayer.RenderTransform = new TransformGroup { Children = { CapRotate, CapLift } };
        FaceLayer.RenderTransform = FaceShift;
        BlushLeft.RenderTransform = BlushLeftShift;
        BlushRight.RenderTransform = BlushRightShift;
        WitchHat.RenderTransform = HatShift;
        EyesLayer.RenderTransform = EyeLids;
        Pupils.RenderTransform = Gaze;
        BrowLeft.RenderTransform = new TransformGroup { Children = { BrowLeftRotate, BrowLeftLift } };
        BrowRight.RenderTransform = new TransformGroup { Children = { BrowRightRotate, BrowRightLift } };
        MouthOpen.RenderTransform = MouthOpenScale;
        Bat.RenderTransform = BatMove;
        BatWingsLayer.RenderTransform = BatWings;
        _frameTimer.Tick += (_, _) => Frame();

        CharacterId = Pasty.Core.Characters.Characters.PastyId;
        ApplyPose(PastyPose.Rest);
    }

    public PastyAnimator Animator { get; } = new();

    public PastyState State => Animator.State;

    /// <summary>Which character's body to wear (an id from Pasty.Core.Characters).</summary>
    public string CharacterId
    {
        get => _characterId;
        set
        {
            if (value == _characterId && _body is not null) return;
            _characterId = value;
            _body = BodyCatalog.Create(value);
            var (back, top) = _body.Detach();
            BodyHost.Children.Clear();
            BodyHost.Children.Add(back);
            TopperHost.Children.Clear();
            if (top is not null) TopperHost.Children.Add(top);

            LeftArm.IsVisible = _body.HasLeftArm;
            LeftArmShift.X = _body.ArmInset;
            RightArmShift.X = -_body.ArmInset;
            FaceShift.Y = _body.FaceOffsetY;
            BlushLeftShift.X = _body.BlushInset;
            BlushRightShift.X = -_body.BlushInset;
            HatShift.Y = _body.HatDrop;
            Season = _season;
        }
    }

    /// <summary>Seasonal costume. Halloween: witch hat (in place of the topper), fangs, and a bat friend.</summary>
    public Season Season
    {
        get => _season;
        set
        {
            _season = value;
            var halloween = value == Season.Halloween;
            WitchHat.IsVisible = halloween;
            TopperHost.IsVisible = !halloween;
            _body?.ApplySeason(value);
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

    /// <summary>Start or stop animating (stop while hidden: zero CPU when nobody can see the character).</summary>
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

    /// <summary>Freezes the avatar in a given pose (Store screenshots, previews).</summary>
    internal void ShowPose(in PastyPose pose) => ApplyPose(pose);

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

        _body?.Animate(p, _clock.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// The whole character, centred in a <paramref name="width"/>×<paramref name="height"/> image with
    /// <paramref name="padding"/> (fraction of the height) above and below. Used for Store tiles.
    /// </summary>
    public static Bitmap RenderFullBody(int width, int height, double padding = 0.12, Season season = Season.None, string? characterId = null)
    {
        var art = Detached(Create(season, characterId));
        // Artwork spans x 8–132 and y 5–183 (topper to shadow); fit that box.
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
        return Render(art, width, height);
    }

    /// <summary>A square head-and-shoulders portrait for tray and window icons and the character picker.</summary>
    public static Bitmap RenderPortrait(int size, Season season = Season.None, string? characterId = null)
    {
        var avatar = Create(season, characterId);
        var body = avatar._body!;
        // The witch hat is taller than any topper, so the Halloween portrait frames a little higher and wider.
        var halloween = season == Season.Halloween;
        var span = halloween ? 124.0 : 104.0;
        var top = halloween ? -16 + body.HatDrop : body.PortraitTop;
        var scale = size / span;
        var art = Detached(avatar);
        art.RenderTransformOrigin = RelativePoint.TopLeft;
        art.RenderTransform = new TransformGroup
        {
            Children = { new TranslateTransform(-(70 - span / 2), -top), new ScaleTransform(scale, scale) },
        };
        return Render(art, size, size);
    }

    private static PastyAvatar Create(Season season, string? characterId)
    {
        var avatar = new PastyAvatar();
        if (characterId is not null) avatar.CharacterId = characterId;
        avatar.Season = season;
        return avatar;
    }

    // Render the artwork canvas itself: an off-screen UserControl has no template applied and draws nothing.
    private static Control Detached(PastyAvatar avatar)
    {
        var art = (Control)avatar.Content!;
        avatar.Content = null;
        return art;
    }

    private static Bitmap Render(Control art, int width, int height)
    {
        var frame = new Canvas { Width = width, Height = height, ClipToBounds = true, Children = { art } };
        frame.Measure(new Size(width, height));
        frame.Arrange(new Rect(0, 0, width, height));
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(frame);
        return bitmap;
    }
}
