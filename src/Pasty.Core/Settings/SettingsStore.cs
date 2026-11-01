using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pasty.Core.Settings;

/// <summary>Loads and saves <see cref="PastySettings"/> as JSON. A corrupt file falls back to defaults.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public SettingsStore(string filePath) => FilePath = filePath;

    public string FilePath { get; }

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pasty");

    public PastySettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<PastySettings>(File.ReadAllText(FilePath), Json) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Pasty would rather forget your preferences than crash on startup.
        }
        return new PastySettings();
    }

    public void Save(PastySettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
        // On Unix, settings may hold an API key fallback: owner read/write only.
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temp, FilePath, overwrite: true);
    }
}
