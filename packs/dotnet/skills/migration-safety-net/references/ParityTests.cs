// Starting point for HTTP parity tests (xUnit, net10.0).
//   PARITY_MODE=record PARITY_BASE_URL=http://localhost:5100 dotnet test   -> writes baseline/*.txt from the OLD app
//   PARITY_BASE_URL=http://localhost:5200 dotnet test                       -> verifies the YARP host / new app
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ParityTests;

public sealed record Scenario(
    string Id,
    string Method,
    string Path,
    string Compare,
    string? Select = null,
    string? Accept = null,
    string[]? Ignore = null);

public class HttpParityTests
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("PARITY_BASE_URL") ?? "http://localhost:5100";
    private static readonly bool Record = Environment.GetEnvironmentVariable("PARITY_MODE") == "record";
    private static readonly string Root = AppContext.BaseDirectory;

    // Old login URL -> new login URL, so redirects compare equal while the login page moves.
    private static readonly Dictionary<string, string> LoginUrlMap = new()
    {
        ["/Account/Login.aspx"] = "/Account/Login",
    };

    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(60),
    };

    public static TheoryData<string> ScenarioIds()
    {
        var data = new TheoryData<string>();
        foreach (var s in Load()) data.Add(s.Id);
        return data;
    }

    [Theory]
    [MemberData(nameof(ScenarioIds))]
    public async Task Same_behavior_as_the_old_app(string id)
    {
        var scenario = Load().Single(s => s.Id == id);
        var actual = await Capture(scenario);
        var baselineFile = Path.Combine(Root, "baseline", scenario.Id + ".txt");

        if (Record)
        {
            // Write next to the project, not into bin/, so the baseline gets committed.
            var projectBaseline = Path.Combine(ProjectDir(), "baseline");
            Directory.CreateDirectory(projectBaseline);
            await File.WriteAllTextAsync(Path.Combine(projectBaseline, scenario.Id + ".txt"), actual);
            return;
        }

        Assert.True(File.Exists(baselineFile), $"No baseline for '{scenario.Id}'. Record it against the old app first.");
        var expected = await File.ReadAllTextAsync(baselineFile);
        Assert.Equal(expected, actual);
    }

    private static async Task<string> Capture(Scenario s)
    {
        using var request = new HttpRequestMessage(new HttpMethod(s.Method), s.Path);
        if (s.Accept is not null) request.Headers.TryAddWithoutValidation("Accept", s.Accept);
        using var response = await Http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;
        var media = response.Content.Headers.ContentType?.MediaType ?? "";

        return s.Compare switch
        {
            "status" => $"STATUS: {status}",
            "status+location" => $"STATUS: {status}\nLOCATION: {Normalizer.Location(response.Headers.Location?.ToString(), LoginUrlMap)}",
            "json" => $"STATUS: {status}\n{Normalizer.Json(body)}",
            "bytes-type" => $"STATUS: {status}\nTYPE: {media}\nNON-EMPTY: {(await response.Content.ReadAsByteArrayAsync()).Length > 0}",
            _ => $"STATUS: {status}\n{Normalizer.Html(body, s.Select ?? "main", s.Ignore ?? [])}",
        };
    }

    private static List<Scenario> Load() =>
        JsonSerializer.Deserialize<List<Scenario>>(
            File.ReadAllText(Path.Combine(Root, "scenarios.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static string ProjectDir()
    {
        var dir = new DirectoryInfo(Root);
        while (dir is not null && !dir.GetFiles("*.csproj").Any()) dir = dir.Parent;
        return dir?.FullName ?? Root;
    }
}

// In the .csproj:
//   <ItemGroup>
//     <None Include="scenarios.json" CopyToOutputDirectory="PreserveNewest" />
//     <None Include="baseline\**\*" CopyToOutputDirectory="PreserveNewest" />
//   </ItemGroup>
