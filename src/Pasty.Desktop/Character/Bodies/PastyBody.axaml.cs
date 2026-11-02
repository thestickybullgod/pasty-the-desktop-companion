using Avalonia.Controls;
using Avalonia.Media;
using Pasty.Core.Seasons;

namespace Pasty.Desktop.Character.Bodies;

public partial class PastyBody : CharacterBody
{
    public PastyBody() => InitializeComponent();

    public override Control Back => BackLayer;

    public override Control? Topper => TopLayer;

    /// <summary>Halloween Edition: orange-and-black label.</summary>
    public override void ApplySeason(Season season)
    {
        var halloween = season == Season.Halloween;
        Band.Fill = new SolidColorBrush(Color.Parse(halloween ? "#FF7A1A" : "#FF6B57"));
        BandSwirl.Stroke = new SolidColorBrush(Color.Parse(halloween ? "#1A1410" : "#FFFFFF"));
    }
}
