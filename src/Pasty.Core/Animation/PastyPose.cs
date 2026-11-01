namespace Pasty.Core.Animation;

public enum MouthShape
{
    Smile,
    Flat,
    Smirk,
    Open,
}

/// <summary>
/// Everything needed to draw one frame of Pasty. Units are in the artwork's own
/// coordinate space (140×184): offsets in units, angles in degrees, scales as factors.
/// </summary>
public record struct PastyPose
{
    public double BodyY;
    public double Lean;
    public double SquashX;
    public double SquashY;
    public double ShadowScale;
    /// <summary>Whole-figure opacity: 1 normally, lower while doing the Halloween ghost bit.</summary>
    public double Opacity;

    public double EyeLid;
    public double GazeX;
    public double GazeY;

    public double BrowLeftAngle;
    public double BrowLeftLift;
    public double BrowRightAngle;
    public double BrowRightLift;

    public double Blush;
    public MouthShape Mouth;
    /// <summary>Vertical scale of the open mouth while talking (0.3–1).</summary>
    public double MouthOpen;

    public double ArmAngle;
    public double CapTilt;
    public double CapLift;
    public double Zzz;

    /// <summary>Halloween Edition: the little bat friend. Opacity, position (artwork units) and wing flap (0–1).</summary>
    public double Bat;
    public double BatX;
    public double BatY;
    public double BatFlap;

    public static PastyPose Rest => new()
    {
        SquashX = 1, SquashY = 1, ShadowScale = 1, Opacity = 1, EyeLid = 1, MouthOpen = 1, Mouth = MouthShape.Smile,
    };
}
