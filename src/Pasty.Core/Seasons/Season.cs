namespace Pasty.Core.Seasons;

public enum Season
{
    None,
    Halloween,
}

/// <summary>What the user picked in Settings → Personality → Seasonal costumes.</summary>
public enum SeasonalMode
{
    /// <summary>Dress up automatically when the date is right.</summary>
    Auto,
    /// <summary>It's Halloween every day. We don't judge.</summary>
    AlwaysHalloween,
    Off,
}

public static class SeasonCalendar
{
    /// <summary>Halloween season: October 15 through November 1 (for the leftover candy).</summary>
    public static Season For(DateTime date, SeasonalMode mode) => mode switch
    {
        SeasonalMode.Off => Season.None,
        SeasonalMode.AlwaysHalloween => Season.Halloween,
        _ when (date.Month == 10 && date.Day >= 15) || (date.Month == 11 && date.Day == 1) => Season.Halloween,
        _ => Season.None,
    };

    public static bool IsHalloweenNight(DateTime date) => date is { Month: 10, Day: 31 };
}
