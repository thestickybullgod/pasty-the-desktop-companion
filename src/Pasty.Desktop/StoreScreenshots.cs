using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Pasty.Core;
using Pasty.Core.Animation;
using Pasty.Core.Seasons;
using Pasty.Desktop.Character;

namespace Pasty.Desktop;

/// <summary>
/// <c>Pasty --render-store-screenshots &lt;dir&gt;</c>: renders 1920×1080 Store screenshots from the real
/// avatar, speech bubble and animation poses, without capturing anyone's actual desktop.
/// </summary>
internal static class StoreScreenshots
{
    public const string Argument = "--render-store-screenshots";

    private const int Width = 1920;
    private const int Height = 1080;

    private sealed record Scene(
        string File, string Headline, string Subhead, string[] Points,
        string Bubble, PastyReaction Reaction, Season Season,
        string BackgroundFrom, string BackgroundTo, string HeadlineColor, string TextColor);

    private static readonly Scene[] Scenes =
    [
        new("1-meet-pasty.png", "Pasty the Desktop Companion", "A desktop buddy with opinions.",
            ["Lives on your desktop: drag it anywhere", "Works completely offline", "Tips, jokes and a sarcasm slider", "Free and open source"],
            "Hi! I'm Pasty, your new desktop companion. Drag me anywhere, double-click me to talk, right-click for options.",
            PastyReaction.Wave, Season.None, "#E3F4F1", "#FFF6DD", "#123B37", "#2E4A47"),

        new("2-bring-your-own-brain.png", "Bring your own brain", "Or none at all: Pasty's own personality works offline.",
            ["Ollama runs entirely on your PC", "Or connect Claude, ChatGPT, Gemini or GitHub Models", "Any OpenAI-compatible endpoint", "API keys stay encrypted on your device"],
            "Press Win+V to open your clipboard history. I'd know. I am, spiritually, a clipboard.",
            PastyReaction.Smug, Season.None, "#ECEFFC", "#E4F6F2", "#1F2A5C", "#3A4466"),

        new("3-private-by-design.png", "Private by design", "Pasty collects nothing. No accounts, no telemetry.",
            ["App and clipboard awareness are opt-in", "What Pasty notices becomes a category, then is forgotten", "Naps when you're away, hides during full-screen apps", "Settings that feel at home on Windows 11"],
            "I don't read your clipboard. I just notice that you copied something and get unreasonably excited.",
            PastyReaction.Happy, Season.None, "#F4F4F4", "#E6EEFB", "#202A36", "#3D4856"),

        new("4-halloween-edition.png", "Halloween Edition", "Born on Halloween 2026. Spooky, never scary.",
            ["A witch hat, fangs and a bat friend", "Ghost mode with /boo", "Seasonal jokes and /trickortreat", "On automatically from Oct 15 to Nov 1"],
            "Halloween is over. I refuse to remove the hat.",
            PastyReaction.Smug, Season.Halloween, "#170D22", "#3B1D0B", "#FF9F1C", "#F3DCC4"),
    ];

    public static async Task RenderAllAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var scene in Scenes)
        {
            var content = Build(scene);
            // A real (off-screen) window gives the controls their theme and templates before rendering.
            var host = new Window
            {
                Width = Width, Height = Height, Content = content, ShowInTaskbar = false, ShowActivated = false,
                WindowDecorations = WindowDecorations.None, Position = new PixelPoint(-32000, -32000),
            };
            host.Show();
            await Task.Delay(400);
            content.Measure(new Size(Width, Height));
            content.Arrange(new Rect(0, 0, Width, Height));

            using var bitmap = new RenderTargetBitmap(new PixelSize(Width, Height), new Vector(96, 96));
            bitmap.Render(content);
            using (var file = File.Create(System.IO.Path.Combine(directory, scene.File)))
                bitmap.Save(file, new PngBitmapEncoderOptions());
            host.Close();
        }
    }

    private static Control Build(Scene scene)
    {
        IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(150, 0, 0, 0), Spacing = 18, MaxWidth = 900 };
        text.Children.Add(new TextBlock { Text = scene.Headline, FontSize = 76, FontWeight = FontWeight.Bold, Foreground = Solid(scene.HeadlineColor), TextWrapping = TextWrapping.Wrap });
        text.Children.Add(new TextBlock { Text = scene.Subhead, FontSize = 36, Foreground = Solid(scene.TextColor), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 24) });
        foreach (var point in scene.Points)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
            row.Children.Add(new Ellipse { Width = 14, Height = 14, Fill = Solid(scene.HeadlineColor), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = point, FontSize = 30, Foreground = Solid(scene.TextColor), TextWrapping = TextWrapping.Wrap, MaxWidth = 820 });
            text.Children.Add(row);
        }

        // The real speech bubble, lifted out of its window.
        var bubbleWindow = new BubbleWindow { Spooky = scene.Season == Season.Halloween };
        var bubble = (Control)bubbleWindow.Content!;
        bubbleWindow.Content = null;
        bubbleWindow.Message.Text = scene.Bubble;
        bubbleWindow.Root.Opacity = 1;
        bubbleWindow.Root.Transitions = null;

        var avatar = new PastyAvatar { Season = scene.Season, Margin = new Thickness(12, 46, 12, 0), ClipToBounds = false };
        avatar.ShowPose(PoseFor(scene));

        var character = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 70) };
        character.Children.Add(new LayoutTransformControl { LayoutTransform = new ScaleTransform(1.9, 1.9), Child = bubble, HorizontalAlignment = HorizontalAlignment.Center, ClipToBounds = false });
        character.Children.Add(new LayoutTransformControl { LayoutTransform = new ScaleTransform(2.6, 2.6), Child = avatar, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -40, 0, 0), ClipToBounds = false });

        var grid = new Grid
        {
            Width = Width, Height = Height,
            ColumnDefinitions = new ColumnDefinitions("1.15*,1*"),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse(scene.BackgroundFrom), 0), new GradientStop(Color.Parse(scene.BackgroundTo), 1) },
            },
        };
        grid.Children.Add(text);
        Grid.SetColumn(character, 1);
        grid.Children.Add(character);
        return grid;
    }

    /// <summary>Plays the animator forward to a flattering moment of the scene's reaction.</summary>
    private static PastyPose PoseFor(Scene scene)
    {
        var animator = new PastyAnimator(new Random(7)) { BatEnabled = scene.Season == Season.Halloween };
        var step = TimeSpan.FromSeconds(1 / 30.0);
        void Advance(double seconds) { for (var t = 0.0; t < seconds; t += step.TotalSeconds) animator.Update(step); }

        if (scene.Season == Season.Halloween)
            Advance(12.35); // the bat's first flight starts at 12 s; this catches it out beside the hat
        animator.React(scene.Reaction);
        Advance(scene.Reaction == PastyReaction.Wave ? 0.75 : 0.9);
        // Animator.Pose is the pose without a blink in progress.
        return animator.Pose;
    }
}
