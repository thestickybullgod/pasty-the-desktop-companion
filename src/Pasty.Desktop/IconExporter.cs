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

    private static void Save(Bitmap bitmap, string path)
    {
        using var file = File.Create(path);
        bitmap.Save(file);
    }

    /// <summary>A PNG-compressed .ico (supported since Windows Vista) with several sizes.</summary>
    private static void WriteIco(Season season, string path)
    {
        var images = IcoSizes.Select(size =>
        {
            using var stream = new MemoryStream();
            PastyAvatar.RenderPortrait(size, season).Save(stream);
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
