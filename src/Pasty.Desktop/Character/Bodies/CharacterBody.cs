using Avalonia.Controls;
using Pasty.Core.Animation;
using Pasty.Core.Seasons;

namespace Pasty.Desktop.Character.Bodies;

/// <summary>
/// A character's look on the shared rig. Each body's XAML has a <c>BackLayer</c> canvas (drawn behind
/// the face) and may have a <c>TopLayer</c> (the "topper", which tips like a hat when waving and is
/// replaced by the witch hat at Halloween). Everything is drawn in the rig's 140×184 artwork space,
/// with the face around x 40–100, y 55–115 unless <see cref="FaceOffsetY"/> moves it.
/// </summary>
public class CharacterBody : Canvas
{
    /// <summary>Artwork behind the face.</summary>
    public virtual Control Back => throw new NotImplementedException("A body must provide a BackLayer.");

    /// <summary>Cap, antenna, sprouts… or null.</summary>
    public virtual Control? Topper => null;

    /// <summary>Moves both arms toward the middle, for narrow bodies.</summary>
    public virtual double ArmInset => 0;

    /// <summary>False when something (like a mug handle) takes the left arm's place.</summary>
    public virtual bool HasLeftArm => true;

    /// <summary>Moves the shared face down, for bodies with something in the way up top.</summary>
    public virtual double FaceOffsetY => 0;

    /// <summary>
    /// How far the brows may rise (artwork units) before they'd leave the face area, e.g. off a phone's
    /// screen or under a copier's lid. Raised brows stop here; their tilt still carries the expression.
    /// </summary>
    public virtual double BrowHeadroom => 12;

    /// <summary>Moves the blush toward the middle so it stays on narrow bodies.</summary>
    public virtual double BlushInset => 0;

    /// <summary>Moves the witch hat down onto a body whose top is lower than Pasty's cap.</summary>
    public virtual double HatDrop => 0;

    /// <summary>Top of the head-and-shoulders crop used for tray and window icons (artwork y).</summary>
    public virtual double PortraitTop => -2;

    /// <summary>Seasonal tweaks to the body itself (the rig handles the hat, fangs and bat).</summary>
    public virtual void ApplySeason(Season season)
    {
    }

    /// <summary>Per-frame extras driven by the pose, e.g. steam that shows a mug's mood.</summary>
    public virtual void Animate(in PastyPose pose, double seconds)
    {
    }

    /// <summary>Lifts the layers out of this canvas so the rig can host them.</summary>
    public (Control Back, Control? Top) Detach()
    {
        var back = Back;
        var top = Topper;
        Children.Remove(back);
        if (top is not null) Children.Remove(top);
        return (back, top);
    }
}
