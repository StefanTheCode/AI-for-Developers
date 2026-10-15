using System.Text.Json.Serialization;

namespace WeatherDashboard.Models;

// =============================================================================
// These records mirror the JSON returned by the Open-Meteo APIs.
//
// LESSON — map JSON explicitly.
// The API uses snake_case names like "temperature_2m". We use [JsonPropertyName]
// so our C# stays idiomatic (PascalCase) while still binding correctly. Records
// give us immutable, read-only data holders in a single line each.
// =============================================================================

/// <summary>Response from the geocoding API (city name -> coordinates).</summary>
public record GeocodingResponse(
    [property: JsonPropertyName("results")] GeoResult[]? Results);

/// <summary>A single matched place.</summary>
public record GeoResult(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("admin1")] string? Admin1);

/// <summary>Response from the forecast API — we only ask for the "current" block.</summary>
public record ForecastResponse(
    [property: JsonPropertyName("current")] CurrentWeather Current);

/// <summary>The current conditions.</summary>
public record CurrentWeather(
    [property: JsonPropertyName("time")] string Time,
    [property: JsonPropertyName("temperature_2m")] double Temperature,
    [property: JsonPropertyName("relative_humidity_2m")] int Humidity,
    [property: JsonPropertyName("wind_speed_10m")] double WindSpeed,
    [property: JsonPropertyName("weather_code")] int WeatherCode);
