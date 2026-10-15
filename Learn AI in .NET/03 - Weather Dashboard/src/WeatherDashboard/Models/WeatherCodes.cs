namespace WeatherDashboard.Models;

/// <summary>
/// Translates WMO weather codes (what the API returns) into human text + an emoji.
/// See https://open-meteo.com/en/docs for the full table.
/// </summary>
public static class WeatherCodes
{
    public static string Describe(int code) => code switch
    {
        0 => "Clear sky ☀️",
        1 or 2 or 3 => "Partly cloudy ⛅",
        45 or 48 => "Fog 🌫️",
        51 or 53 or 55 => "Drizzle 🌦️",
        61 or 63 or 65 => "Rain 🌧️",
        66 or 67 => "Freezing rain 🌧️❄️",
        71 or 73 or 75 => "Snow 🌨️",
        77 => "Snow grains 🌨️",
        80 or 81 or 82 => "Rain showers 🌧️",
        85 or 86 => "Snow showers 🌨️",
        95 => "Thunderstorm ⛈️",
        96 or 99 => "Thunderstorm with hail ⛈️",
        _ => $"Unknown (code {code})"
    };
}
