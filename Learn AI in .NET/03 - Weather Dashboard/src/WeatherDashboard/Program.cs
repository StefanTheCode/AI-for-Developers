using WeatherDashboard.Models;
using WeatherDashboard.Services;

// =============================================================================
// Weather Dashboard — Starter project #3
// Focus: HttpClient + async / await.
//
// Flow:  city name  --geocode-->  coordinates  --forecast-->  current weather
// Data source: Open-Meteo (https://open-meteo.com) — free, no API key.
//
// Note the `await Main` style: the entry point is async, so I/O never blocks a
// thread. We create ONE HttpClient and reuse it for every call.
// =============================================================================

// You can pass a city as an argument:  dotnet run -- "Novi Sad"
// Otherwise we prompt for one (falling back to Belgrade).
var city = args.Length > 0
    ? string.Join(' ', args)
    : AskForCity();

// One HttpClient for the whole program. `using` disposes it on exit.
// Timeout is a safety net; we ALSO pass a CancellationToken per call below.
using var http = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(15)
};
var weather = new WeatherClient(http);

// A CancellationToken that trips after 10s so a hung network call can't freeze us.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    Console.WriteLine($"🔎 Looking up '{city}'...");

    var place = await weather.GeocodeAsync(city, cts.Token);
    if (place is null)
    {
        Console.WriteLine($"❌ Couldn't find a place called '{city}'. Try another spelling.");
        return 1;
    }

    Console.WriteLine($"📍 {place.Name}, {place.Admin1}, {place.Country} " +
                      $"({place.Latitude:0.00}, {place.Longitude:0.00})");
    Console.WriteLine("🌐 Fetching current weather...");

    var current = await weather.GetCurrentWeatherAsync(place.Latitude, place.Longitude, cts.Token);

    PrintWeather(place, current);
    return 0;
}
// LESSON — handle the failure modes explicitly. Networks fail; good code expects it.
catch (OperationCanceledException)
{
    Console.WriteLine("⏱️  The request timed out. Check your connection and try again.");
    return 2;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"🌐 Network error: {ex.Message}");
    return 3;
}

// --- helpers -----------------------------------------------------------------

static string AskForCity()
{
    Console.Write("Enter a city (default: Belgrade): ");
    var input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? "Belgrade" : input.Trim();
}

static void PrintWeather(GeoResult place, CurrentWeather w)
{
    Console.WriteLine();
    Console.WriteLine("┌───────────────────────────────────┐");
    Console.WriteLine($"│ Weather in {place.Name,-22} │");
    Console.WriteLine("├───────────────────────────────────┤");
    Console.WriteLine($"│ {WeatherCodes.Describe(w.WeatherCode),-33} │");
    Console.WriteLine($"│ 🌡️  Temperature : {w.Temperature,6:0.0} °C        │");
    Console.WriteLine($"│ 💧 Humidity    : {w.Humidity,6} %         │");
    Console.WriteLine($"│ 💨 Wind        : {w.WindSpeed,6:0.0} km/h      │");
    Console.WriteLine("└───────────────────────────────────┘");
    Console.WriteLine($"(observed at {w.Time})");
}
