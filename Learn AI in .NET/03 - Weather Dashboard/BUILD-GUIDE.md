# 03 · Weather Dashboard — Build Guide

> **Level:** Starter · **Time:** 2–4 hours · **You'll need:** .NET 10 SDK, an AI assistant, and internet (Open-Meteo is free and needs no key)
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

A console app: city name → coordinates → current weather → a small dashboard.

**The real goal:** `async`/`await` stops being magic, and you learn the `HttpClient` mistakes that only show up in production.

---

## Step-by-step

### Step 1 — Look at the raw JSON first

Before any C#, open these in a browser:

- `https://geocoding-api.open-meteo.com/v1/search?name=Belgrade&count=1`
- `https://api.open-meteo.com/v1/forecast?latitude=44.8&longitude=20.47&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code`

You can't review the AI's models if you've never seen the data they map.

### Step 2 — Records for the JSON

> **Prompt:** "Here are two JSON responses [paste]. Create C# records that map them using `[JsonPropertyName]` so my properties stay PascalCase. Only map the fields I use."

### Step 3 — A client class that *receives* its `HttpClient`

> **Prompt:** "Create a `WeatherClient` that takes an `HttpClient` in its constructor, and has `GeocodeAsync(city, ct)` and `GetCurrentWeatherAsync(lat, lon, ct)`. Use `GetFromJsonAsync`. Every method takes a `CancellationToken`. Do NOT create an HttpClient inside the class."

**Check yourself:** Why is `new HttpClient()` per call a problem, even though `HttpClient` is `IDisposable`?

### Step 4 — Build URLs safely

Two things to check in the AI's code: `Uri.EscapeDataString(city)` and `latitude.ToString(CultureInfo.InvariantCulture)`.

**Check yourself:** What does `44.8.ToString()` print on a machine set to Serbian regional settings?

### Step 5 — `Program.cs`: async Main, timeout, failure handling

> **Prompt:** "Write the top-level program: read the city from args or a prompt, create ONE HttpClient, use a `CancellationTokenSource` with a 10-second timeout, and handle `OperationCanceledException` and `HttpRequestException` separately with different exit codes."

### Step 6 — WMO weather codes → text

A `switch` expression that maps codes to descriptions. Easy work to hand to the AI, but **check the codes against the Open-Meteo docs.** AI invents codes.

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| `using var client = new HttpClient();` inside the method | Socket exhaustion under load | One reused client, or `IHttpClientFactory` |
| `.Result` / `.Wait()` "to keep it simple" | Blocks threads and can deadlock | `await` all the way up |
| `$"...latitude={lat}"` with a raw `double` | Serbian/German culture writes `44,8`, and the API breaks | `InvariantCulture` |
| Forget `Uri.EscapeDataString` | "Novi Sad" and "São Paulo" break the query | Escape user input |
| `catch (Exception) { }` | Hides timeouts *and* bugs | Catch the specific failure modes |
| Ignore `CancellationToken` | A hung network call freezes the app | Pass `ct` everywhere |

---

## 🔨 Break it on purpose

1. Remove `InvariantCulture`, add `CultureInfo.CurrentCulture = new("sr-Latn-RS");`, and run. What does the API return?
2. Set the timeout to `TimeSpan.FromMilliseconds(1)`. Which `catch` fires?
3. Turn off Wi-Fi and run again. Which one fires now?
4. Search for `"asdfgh"`. Does the app handle "no results"?

---

## ✅ Definition of done

- [ ] Works for cities with spaces and special characters
- [ ] Works on a machine with non-English regional settings
- [ ] Timeouts and network errors show friendly messages with different exit codes
- [ ] There is exactly one `new HttpClient()` in the whole app
- [ ] You can explain what `await` does to the thread while the request is in flight
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Turn it into a Minimal API endpoint `GET /weather?city=...`, register `WeatherClient` as a typed client with `IHttpClientFactory`, and add `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler()`) for retries.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
