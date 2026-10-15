using System.Globalization;
using System.Net.Http.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services;

/// <summary>
/// A "typed client" that wraps <see cref="HttpClient"/> and talks to Open-Meteo.
///
/// LESSON — how to use HttpClient correctly.
/// * We take an <see cref="HttpClient"/> via the constructor and REUSE it. We do
///   NOT `new HttpClient()` per request — doing that exhausts OS sockets under
///   load (the single most common HttpClient bug). In a real app you'd register
///   this as a typed client with <c>IHttpClientFactory</c>.
/// * Every network method is <c>async</c> and takes a <see cref="CancellationToken"/>
///   so calls can time out or be cancelled — never block a thread on I/O.
/// * <c>GetFromJsonAsync</c> streams the response straight into our records.
/// </summary>
public sealed class WeatherClient
{
    private readonly HttpClient _http;

    public WeatherClient(HttpClient http) => _http = http;

    /// <summary>Turns a city name into coordinates. Returns null if not found.</summary>
    public async Task<GeoResult?> GeocodeAsync(string city, CancellationToken ct = default)
    {
        // Uri.EscapeDataString protects against odd characters/spaces in input.
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";

        var response = await _http.GetFromJsonAsync<GeocodingResponse>(url, ct);

        // The API returns an object with a possibly-empty/absent results array.
        return response?.Results is { Length: > 0 } results ? results[0] : null;
    }

    /// <summary>Fetches the current conditions for a coordinate pair.</summary>
    public async Task<CurrentWeather> GetCurrentWeatherAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        // InvariantCulture so the decimal point is always '.', regardless of the
        // machine's locale — a subtle bug when building URLs with numbers.
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code";

        var forecast = await _http.GetFromJsonAsync<ForecastResponse>(url, ct)
            ?? throw new InvalidOperationException("The forecast API returned no data.");

        return forecast.Current;
    }
}
