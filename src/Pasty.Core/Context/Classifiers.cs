using System.Text.RegularExpressions;

namespace Pasty.Core.Context;

/// <summary>Maps a process name to a coarse category. Window titles are never inspected.</summary>
public static class AppClassifier
{
    public const string Code = "code";
    public const string Terminal = "terminal";
    public const string Writing = "writing";
    public const string Spreadsheet = "spreadsheet";
    public const string Slides = "slides";
    public const string Browser = "browser";
    public const string Chat = "chat";
    public const string Email = "email";
    public const string Gaming = "gaming";
    public const string Settings = "settings";
    public const string Other = "other";

    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["devenv"] = Code, ["code"] = Code, ["code - insiders"] = Code, ["rider64"] = Code,
        ["idea64"] = Code, ["pycharm64"] = Code, ["cursor"] = Code, ["sublime_text"] = Code, ["notepad++"] = Code,
        ["windowsterminal"] = Terminal, ["wt"] = Terminal, ["pwsh"] = Terminal, ["powershell"] = Terminal,
        ["cmd"] = Terminal, ["conhost"] = Terminal,
        ["winword"] = Writing, ["notepad"] = Writing, ["wordpad"] = Writing, ["obsidian"] = Writing,
        ["excel"] = Spreadsheet,
        ["powerpnt"] = Slides,
        ["msedge"] = Browser, ["chrome"] = Browser, ["firefox"] = Browser, ["brave"] = Browser, ["opera"] = Browser,
        ["ms-teams"] = Chat, ["teams"] = Chat, ["slack"] = Chat, ["discord"] = Chat,
        ["outlook"] = Email, ["olk"] = Email, ["thunderbird"] = Email,
        ["steam"] = Gaming, ["steamwebhelper"] = Gaming, ["epicgameslauncher"] = Gaming,
        ["systemsettings"] = Settings, ["mmc"] = Settings, ["regedit"] = Settings, ["taskmgr"] = Settings,
    };

    public static string Classify(string processName) =>
        Map.TryGetValue(processName.Trim(), out var category) ? category : Other;
}

/// <summary>
/// Reduces clipboard text to a category. The text itself is discarded by the caller
/// immediately after this runs; it is never stored, logged or sent anywhere.
/// </summary>
public static partial class ClipboardClassifier
{
    public const string Url = "url";
    public const string Email = "email";
    public const string Code = "code";
    public const string Number = "number";
    public const string Huge = "huge";
    public const string Text = "text";

    public static string Classify(string text)
    {
        var t = text.Trim();
        if (t.Length > 5000) return Huge;
        if (UrlPattern().IsMatch(t)) return Url;
        if (EmailPattern().IsMatch(t)) return Email;
        if (NumberPattern().IsMatch(t)) return Number;
        if (LooksLikeCode(t)) return Code;
        return Text;
    }

    private static bool LooksLikeCode(string t)
    {
        var signals = 0;
        if (t.Contains(';')) signals++;
        if (t.Contains('{') && t.Contains('}')) signals++;
        if (t.Contains("=>") || t.Contains("->") || t.Contains("==")) signals++;
        if (CodeKeyword().IsMatch(t)) signals++;
        return signals >= 2;
    }

    [GeneratedRegex(@"^(https?|ftp)://\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[-+]?[\d,]*\.?\d+$")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"\b(public|private|class|function|def|return|var|let|const|import|using|#include)\b")]
    private static partial Regex CodeKeyword();
}
