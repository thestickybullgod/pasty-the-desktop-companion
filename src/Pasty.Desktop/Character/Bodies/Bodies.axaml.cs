using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Pasty.Core.Animation;

namespace Pasty.Desktop.Character.Bodies;

// Code-behind for the simpler bodies: just how each one fits the shared rig.

public partial class BrickBody : CharacterBody
{
    public BrickBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double BlushInset => 4;   // keep the blush on the screen
    public override double BrowHeadroom => 5; // brows stay on the screen
    public override double PortraitTop => 0;
}

public partial class StickyBody : CharacterBody
{
    public StickyBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double FaceOffsetY => 6;  // centred on the note
    public override double HatDrop => 8;
    public override double PortraitTop => 26;
}

public partial class FloppyBody : CharacterBody
{
    public FloppyBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override double FaceOffsetY => 6;  // clear of the label's top stripe
    public override double BrowHeadroom => 7;
    public override double HatDrop => 4;
    public override double PortraitTop => 24;
}

public partial class HiliteBody : CharacterBody
{
    public HiliteBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double ArmInset => 9;     // a slim body: bring the arms in to meet it
    public override double BlushInset => 6;
    public override double BrowHeadroom => 7;  // stay below the cap
    public override double HatDrop => 6;
    public override double PortraitTop => 6;
}

public partial class PotatoBody : CharacterBody
{
    public PotatoBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double HatDrop => 6;
    public override double PortraitTop => 18;
}

/// <summary>Mugsy's steam shows its mood: lazy when sleepy, hot and tall when annoyed, rosy when happy.</summary>
public partial class MugsyBody : CharacterBody
{
    private readonly (Avalonia.Controls.Shapes.Path Path, TranslateTransform Sway, ScaleTransform Height, double Phase)[] _steam;
    private Color _steamColor;

    public MugsyBody()
    {
        InitializeComponent();
        _steam = [Wisp(SteamA, 0), Wisp(SteamB, 2.1), Wisp(SteamC, 4.2)];
    }

    public override Control Back => BackLayer;
    public override bool HasLeftArm => false;  // the handle is on the left
    public override double FaceOffsetY => 8;   // below the rim
    public override double BrowHeadroom => 6;  // stay off the coffee
    public override double HatDrop => 12;
    public override double PortraitTop => 8;

    public override void Animate(in PastyPose pose, double seconds)
    {
        var sleepy = pose.EyeLid < 0.4;
        var annoyed = pose.BrowLeftAngle > 8;
        var surprised = pose.EyeLid > 1.08;
        var happy = pose.Blush > 0.3;

        var speed = annoyed ? 2.4 : sleepy ? 0.35 : 1.0;
        var height = annoyed ? 1.3 : surprised ? 1.35 : sleepy ? 0.55 : 1.0;
        var opacity = sleepy ? 0.3 : 0.85;
        var color = annoyed ? Color.Parse("#FF9A76") : happy ? Color.Parse("#FFC4D0") : Color.Parse("#CFD8DE");

        if (color != _steamColor)
        {
            _steamColor = color;
            var brush = new SolidColorBrush(color);
            foreach (var wisp in _steam) wisp.Path.Stroke = brush;
        }

        foreach (var (path, sway, scale, phase) in _steam)
        {
            sway.X = Math.Sin(seconds * 2 * speed + phase) * 2.5;
            scale.ScaleY = height * (0.92 + 0.08 * Math.Sin(seconds * 3 * speed + phase));
            path.Opacity = opacity;
        }
    }

    private static (Avalonia.Controls.Shapes.Path, TranslateTransform, ScaleTransform, double) Wisp(Avalonia.Controls.Shapes.Path path, double phase)
    {
        var sway = new TranslateTransform();
        var height = new ScaleTransform();
        path.RenderTransform = new TransformGroup { Children = { height, sway } };
        return (path, sway, height, phase);
    }
}

/// <summary>Pasty since 1.3: a bottle of school glue with a pointy orange twist cap (the tube is <see cref="ClassicPastyBody"/>).</summary>
public partial class PastyBody : CharacterBody
{
    public PastyBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double FaceOffsetY => 6;   // clear of the bottle's shoulders
    public override double HatDrop => 16;
    public override double PortraitTop => 6;

    /// <summary>Halloween: an orange label with a black swirl.</summary>
    public override void ApplySeason(Pasty.Core.Seasons.Season season)
    {
        var halloween = season == Pasty.Core.Seasons.Season.Halloween;
        Band.Fill = new SolidColorBrush(Color.Parse(halloween ? "#FF7A1A" : "#FF6B57"));
        BandSwirl.Stroke = new SolidColorBrush(Color.Parse(halloween ? "#1A1410" : "#FFFFFF"));
    }
}

/// <summary>Gluey, Pasty's friend: an orange twist-up glue stick whose glue shows when the cap tips off.</summary>
public partial class GlueyBody : CharacterBody
{
    public GlueyBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double FaceOffsetY => 7;   // clear of the cap's rim
    public override double HatDrop => 8;
    public override double PortraitTop => 4;

    /// <summary>Halloween: a purple label with orange trim.</summary>
    public override void ApplySeason(Pasty.Core.Seasons.Season season)
    {
        var halloween = season == Pasty.Core.Seasons.Season.Halloween;
        Band.Fill = new SolidColorBrush(Color.Parse(halloween ? "#2B1A3F" : "#16181D"));
        var trim = new SolidColorBrush(Color.Parse(halloween ? "#FF7A1A" : "#2CC5B5"));
        EdgeTop.Fill = trim;
        EdgeBottom.Fill = trim;
    }
}

/// <summary>Coppie: a small desktop copier. At Halloween its fresh copy is a jack-o'-lantern.</summary>
public partial class CoppieBody : CharacterBody
{
    public CoppieBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double FaceOffsetY => 12;  // on the front panel, below the lid
    public override double BrowHeadroom => 6;   // raised brows stop short of the lid
    public override double HatDrop => 12;
    public override double PortraitTop => 22;

    public override void ApplySeason(Pasty.Core.Seasons.Season season)
    {
        var halloween = season == Pasty.Core.Seasons.Season.Halloween;
        PrintLines.IsVisible = !halloween;
        Pumpkin.IsVisible = halloween;
    }
}

/// <summary>Posty: an airmail envelope. At Halloween the stamp turns orange and purple.</summary>
public partial class PostyBody : CharacterBody
{
    public PostyBody() => InitializeComponent();
    public override Control Back => BackLayer;
    public override Control? Topper => TopLayer;
    public override double ArmInset => -4;     // a wide body: arms start at its edges
    public override double FaceOffsetY => 6;   // below the flap's hinge
    public override double BrowHeadroom => 8;
    public override double HatDrop => 22;
    public override double PortraitTop => 18;

    public override void ApplySeason(Pasty.Core.Seasons.Season season)
    {
        var halloween = season == Pasty.Core.Seasons.Season.Halloween;
        Stamp.Fill = new SolidColorBrush(Color.Parse(halloween ? "#3A2356" : "#FF6B57"));
        StampMark.Fill = new SolidColorBrush(Color.Parse(halloween ? "#FF9F1C" : "#FFFFFF"));
    }
}

/// <summary>Maps character ids (Pasty.Core.Characters) to their bodies.</summary>
public static class BodyCatalog
{
    public static CharacterBody Create(string? characterId) => characterId switch
    {
        "brick" => new BrickBody(),
        "sticky" => new StickyBody(),
        "floppy" => new FloppyBody(),
        "mugsy" => new MugsyBody(),
        "hilite" => new HiliteBody(),
        "potato" => new PotatoBody(),
        "pasty-classic" => new ClassicPastyBody(),
        "gluey" => new GlueyBody(),
        "coppie" => new CoppieBody(),
        "posty" => new PostyBody(),
        _ => new PastyBody(),
    };
}
