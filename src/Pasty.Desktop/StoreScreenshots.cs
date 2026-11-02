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
            await RenderAsync(Build(scene), Width, Height, System.IO.Path.Combine(directory, scene.File));
        await RenderAsync(BuildLineup(), Width, Height, System.IO.Path.Combine(directory, "5-pick-your-companion.png"));

        // Store key art: 9:16 poster and 1:1 box art, in their own folder next to the screenshots.
        var art = System.IO.Path.Combine(directory, "..", "art");
        Directory.CreateDirectory(art);
        await RenderAsync(BuildKeyArt(1440, 2160), 1440, 2160, System.IO.Path.Combine(art, "poster-art-9x16-1440x2160.png"));
        await RenderAsync(BuildKeyArt(2160, 2160), 2160, 2160, System.IO.Path.Combine(art, "box-art-1x1-2160x2160.png"));
    }

    private static async Task RenderAsync(Control content, int width, int height, string path)
    {
        // A real (off-screen) window gives the controls their theme and templates before rendering.
        var host = new Window
        {
            Width = width, Height = height, Content = content, ShowInTaskbar = false, ShowActivated = false,
            WindowDecorations = WindowDecorations.None, Position = new PixelPoint(-32000, -32000),
        };
        host.Show();
        await Task.Delay(400);
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));

        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(content);
        using (var file = File.Create(path))
            bitmap.Save(file, new PngBitmapEncoderOptions());
        host.Close();
    }

    public const string PreviewArgument = "--render-character-preview";

    /// <summary>
    /// <c>Pasty --render-character-preview &lt;dir&gt; id1,id2</c>: each body in four poses (rest, wave,
    /// smug, Halloween) for comparing character designs. Writes <c>character-preview.png</c>.
    /// </summary>
    public static async Task RenderPreviewAsync(string directory, string[] ids)
    {
        Directory.CreateDirectory(directory);
        IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));
        var rows = new StackPanel { Spacing = 24, Margin = new Thickness(40) };
        var poses = new (string Label, PastyReaction Reaction, Season Season)[]
        {
            ("rest", PastyReaction.None, Season.None),
            ("wave", PastyReaction.Wave, Season.None),
            ("smug", PastyReaction.Smug, Season.None),
            ("surprised", PastyReaction.Surprised, Season.None),
            ("annoyed", PastyReaction.Annoyed, Season.None),
            ("halloween", PastyReaction.Happy, Season.Halloween),
        };
        foreach (var id in ids)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 30 };
            row.Children.Add(new TextBlock { Text = id, FontSize = 28, FontWeight = FontWeight.SemiBold, Width = 180, VerticalAlignment = VerticalAlignment.Center, Foreground = Solid("#123B37") });
            foreach (var (label, reaction, season) in poses)
            {
                var avatar = new PastyAvatar { CharacterId = id, Margin = new Thickness(12, 46, 12, 0), ClipToBounds = false };
                avatar.Season = season;
                avatar.ShowPose(reaction == PastyReaction.None ? PastyPose.Rest : PoseFor(reaction, season));
                var cell = new StackPanel { Spacing = 4 };
                cell.Children.Add(new LayoutTransformControl { LayoutTransform = new ScaleTransform(1.5, 1.5), Child = avatar, ClipToBounds = false });
                cell.Children.Add(new TextBlock { Text = label, FontSize = 18, Foreground = Solid("#2E4A47"), HorizontalAlignment = HorizontalAlignment.Center });
                row.Children.Add(cell);
            }
            rows.Children.Add(row);
        }
        var width = 290 + poses.Length * 276;
        var height = 80 + ids.Length * 360;
        var page = new Grid { Width = width, Height = height, Background = Solid("#F6F3EA"), Children = { rows } };
        await RenderAsync(page, width, height, System.IO.Path.Combine(directory, "character-preview.png"));
    }

    private static string Number(int n) =>
        n is >= 1 and <= 12
            ? new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" }[n - 1]
            : n.ToString();

    /// <summary>All the companions side by side, each with its name: "Pick your companion".</summary>
    private static Control BuildLineup()
    {
        IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));
        var all = Pasty.Core.Characters.Characters.All;
        // Two rows once the cast outgrows one 1920-pixel line.
        var perRow = all.Count > 8 ? (all.Count + 1) / 2 : all.Count;
        var rows = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
        var reactions = new[] { PastyReaction.Wave, PastyReaction.Smug, PastyReaction.Happy, PastyReaction.Surprised, PastyReaction.Happy, PastyReaction.Smug, PastyReaction.Happy };
        for (var i = 0; i < all.Count; i++)
        {
            if (i % perRow == 0)
                rows.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 34 });
            var info = all[i];
            var avatar = new PastyAvatar { CharacterId = info.Id, Margin = new Thickness(12, 30, 12, 0), ClipToBounds = false };
            avatar.ShowPose(PoseFor(reactions[i % reactions.Length], Season.None) with { CapTilt = 0, CapLift = 0 });
            var cell = new StackPanel { Spacing = 2, Width = 230 };
            cell.Children.Add(new LayoutTransformControl { LayoutTransform = new ScaleTransform(1.15, 1.15), Child = avatar, ClipToBounds = false, HorizontalAlignment = HorizontalAlignment.Center });
            cell.Children.Add(new TextBlock { Text = info.Title, FontSize = 32, FontWeight = FontWeight.SemiBold, Foreground = Solid("#123B37"), HorizontalAlignment = HorizontalAlignment.Center });
            ((StackPanel)rows.Children[^1]).Children.Add(cell);
        }

        var layout = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 24 };
        layout.Children.Add(new TextBlock { Text = "Pick your companion", FontSize = 72, FontWeight = FontWeight.Bold, Foreground = Solid("#123B37"), HorizontalAlignment = HorizontalAlignment.Center });
        layout.Children.Add(rows);
        layout.Children.Add(new TextBlock { Text = $"Same personality, {Number(all.Count)} looks. Switch any time in Settings.", FontSize = 32, Foreground = Solid("#2E4A47"), HorizontalAlignment = HorizontalAlignment.Center });

        return new Grid
        {
            Width = Width, Height = Height,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#E3F4F1"), 0), new GradientStop(Color.Parse("#FFF6DD"), 1) },
            },
            Children = { layout },
        };
    }

    /// <summary>
    /// Poster and box art: the full product name over a large waving Pasty, in the brand colours.
    /// Everything sits well inside the edges, since the Store crops and overlays key art in places.
    /// </summary>
    private static Control BuildKeyArt(int width, int height)
    {
        IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));
        var unit = Math.Min(width, height) / 1440.0;   // scale type and character with the canvas
        var portrait = height > width;

        var title = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6 * unit };
        title.Children.Add(new TextBlock { Text = "Pasty", FontSize = 230 * unit, FontWeight = FontWeight.Bold, Foreground = Solid("#123B37"), HorizontalAlignment = HorizontalAlignment.Center });
        title.Children.Add(new TextBlock { Text = "the Desktop Companion", FontSize = 86 * unit, FontWeight = FontWeight.SemiBold, Foreground = Solid("#1F5E57"), HorizontalAlignment = HorizontalAlignment.Center });

        var avatar = new PastyAvatar { Margin = new Thickness(12, 46, 12, 0), ClipToBounds = false };
        // Wave, but keep the cap on: at poster size the hat-tip reads as a lid flying off.
        avatar.ShowPose(PoseFor(PastyReaction.Wave, Season.None) with { CapTilt = 0, CapLift = 0 });
        var characterScale = (portrait ? 6.4 : 3.9) * unit;
        var glowSize = (portrait ? 1180 : 1000) * unit;

        var character = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        character.Children.Add(new Ellipse
        {
            Width = glowSize, Height = glowSize, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Fill = new RadialGradientBrush { GradientStops = { new GradientStop(Color.Parse("#CCFFFFFF"), 0), new GradientStop(Color.Parse("#00FFFFFF"), 1) } },
        });
        character.Children.Add(new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(characterScale, characterScale), Child = avatar,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ClipToBounds = false,
        });

        var layout = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = (portrait ? 40 : 30) * unit,
        };
        layout.Children.Add(title);
        layout.Children.Add(character);
        if (portrait)
            layout.Children.Add(new TextBlock { Text = "A desktop buddy with opinions.", FontSize = 58 * unit, Foreground = Solid("#2E4A47"), HorizontalAlignment = HorizontalAlignment.Center });

        return new Grid
        {
            Width = width, Height = height,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#E3F4F1"), 0), new GradientStop(Color.Parse("#FFF6DD"), 1) },
            },
            Children = { layout },
        };
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
    private static PastyPose PoseFor(Scene scene) => PoseFor(scene.Reaction, scene.Season);

    private static PastyPose PoseFor(PastyReaction reaction, Season season)
    {
        var animator = new PastyAnimator(new Random(7)) { BatEnabled = season == Season.Halloween };
        var step = TimeSpan.FromSeconds(1 / 30.0);
        void Advance(double seconds) { for (var t = 0.0; t < seconds; t += step.TotalSeconds) animator.Update(step); }

        if (season == Season.Halloween)
            Advance(12.35); // the bat's first flight starts at 12 s; this catches it out beside the hat
        animator.React(reaction);
        Advance(reaction == PastyReaction.Wave ? 0.75 : 0.9);
        // Animator.Pose is the pose without a blink in progress.
        return animator.Pose;
    }
}
