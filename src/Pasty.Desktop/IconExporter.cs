using Avalonia.Media.Imaging;
using Pasty.Core.Seasons;
using Pasty.Desktop.Character;

namespace Pasty.Desktop;

/// <summary>
/// <c>Pasty --export-icons &lt;dir&gt;</c>: renders the app icons from Pasty's own vector artwork,
/// so packaging never needs hand-maintained image files.
/// </summary>
internal static class IconExporter
{
    public const string Argument = "--export-icons";

    private static readonly int[] IcoSizes = [256, 64, 48, 32, 16];

    public static void Export(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var (season, suffix) in new[] { (Season.None, ""), (Season.Halloween, "-halloween") })
        {
            Save(PastyAvatar.RenderPortrait(256, season), Path.Combine(directory, $"pasty{suffix}.png"));
            WriteIco(season, Path.Combine(directory, $"pasty{suffix}.ico"));
        }
    }

    public const string StoreAssetsArgument = "--export-store-assets";

    /// <summary>
    /// <c>Pasty --export-store-assets &lt;dir&gt;</c>: every logo and tile the MSIX manifest references,
    /// at the scales Windows picks from. Small sizes use the head portrait (readable at 16 px);
    /// tiles show the whole character.
    /// </summary>
    public static void ExportStoreAssets(string directory)
    {
        Directory.CreateDirectory(directory);
        int[] scales = [100, 200, 400];

        foreach (var scale in scales)
        {
            int Px(int size) => size * scale / 100;
            Save(PastyAvatar.RenderPortrait(Px(50)), Path.Combine(directory, $"StoreLogo.scale-{scale}.png"));
            Save(PastyAvatar.RenderPortrait(Px(44)), Path.Combine(directory, $"Square44x44Logo.scale-{scale}.png"));
            Save(PastyAvatar.RenderFullBody(Px(71), Px(71)), Path.Combine(directory, $"Square71x71Logo.scale-{scale}.png"));
            Save(PastyAvatar.RenderFullBody(Px(150), Px(150)), Path.Combine(directory, $"Square150x150Logo.scale-{scale}.png"));
            Save(PastyAvatar.RenderFullBody(Px(310), Px(150)), Path.Combine(directory, $"Wide310x150Logo.scale-{scale}.png"));
            Save(PastyAvatar.RenderFullBody(Px(310), Px(310)), Path.Combine(directory, $"Square310x310Logo.scale-{scale}.png"));
        }

        // Taskbar / Start / file icons at exact pixel sizes, with and without the tile plate.
        foreach (var size in new[] { 16, 24, 32, 48, 256 })
        {
            Save(PastyAvatar.RenderPortrait(size), Path.Combine(directory, $"Square44x44Logo.targetsize-{size}.png"));
            Save(PastyAvatar.RenderPortrait(size), Path.Combine(directory, $"Square44x44Logo.targetsize-{size}_altform-unplated.png"));
        }
    }

    private static void Save(Bitmap bitmap, string path)
    {
        using var file = File.Create(path);
        bitmap.Save(file, new PngBitmapEncoderOptions());
    }

    /// <summary>A PNG-compressed .ico (supported since Windows Vista) with several sizes.</summary>
    private static void WriteIco(Season season, string path)
    {
        var images = IcoSizes.Select(size =>
        {
            using var stream = new MemoryStream();
            PastyAvatar.RenderPortrait(size, season).Save(stream, new PngBitmapEncoderOptions());
            return (size, data: stream.ToArray());
        }).ToList();

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((short)0);            // reserved
        writer.Write((short)1);            // type: icon
        writer.Write((short)images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (size, data) in images)
        {
            writer.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);         // palette
            writer.Write((byte)0);         // reserved
            writer.Write((short)1);        // colour planes
            writer.Write((short)32);       // bits per pixel
            writer.Write(data.Length);
            writer.Write(offset);
            offset += data.Length;
        }
        foreach (var (_, data) in images)
            writer.Write(data);
    }
}
