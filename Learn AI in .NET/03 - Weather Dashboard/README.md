# 03 · Weather Dashboard

> **Starter project** · Focus: **`HttpClient`** + **`async` / `await`**.

A console app that takes a city name, looks up its coordinates, fetches the
current weather, and prints a little dashboard. It calls the free
[Open-Meteo](https://open-meteo.com) API — **no API key required**.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Using `HttpClient` the RIGHT way (reuse, don't `new` per call) | [`Services/WeatherClient.cs`](src/WeatherDashboard/Services/WeatherClient.cs) |
| `async`/`await` end-to-end, async `Main` | [`Program.cs`](src/WeatherDashboard/Program.cs) |
| Cancellation + timeouts with `CancellationToken` | [`Program.cs`](src/WeatherDashboard/Program.cs) |
| Deserializing JSON into records (`GetFromJsonAsync`) | [`Models/WeatherModels.cs`](src/WeatherDashboard/Models/WeatherModels.cs) |
| Handling real failure modes (timeout, network errors) | [`Program.cs`](src/WeatherDashboard/Program.cs) |

---

## Run it

```bash
cd "03 - Weather Dashboard"

# Prompted for a city:
dotnet run --project src/WeatherDashboard

# Or pass one directly (note the -- separates args from dotnet):
dotnet run --project src/WeatherDashboard -- "Novi Sad"
```

Example output:

```
📍 Belgrade, Central Serbia, Serbia (44.80, 20.47)
┌───────────────────────────────────┐
│ Weather in Belgrade               │
├───────────────────────────────────┤
│ Clear sky ☀️                       │
│ 🌡️  Temperature :   21.4 °C        │
│ 💧 Humidity    :     48 %         │
│ 💨 Wind        :    9.7 km/h      │
└───────────────────────────────────┘
```

> Needs an internet connection at runtime (it calls a live API).

---

## Why the `HttpClient` details matter

The single most common `HttpClient` mistake is `new HttpClient()` inside a loop
or per request. Each new instance opens sockets that linger in `TIME_WAIT`, and
under load you hit **socket exhaustion** — intermittent, hard-to-debug failures.
This project **creates one client and reuses it** (see `WeatherClient`). In an
ASP.NET app you'd go one step further and register a *typed client* with
`IHttpClientFactory`, which manages the socket pool for you.

The other two habits worth stealing:
- **Everything is `async`** and takes a `CancellationToken`, so a slow API can't
  freeze the app — we time out after 10s.
- **Failures are expected**, not exceptional: we catch timeouts and network
  errors and print a friendly message with a distinct exit code.

---

## 🤖 Try these prompts with your AI assistant

**Add a feature:**
> "Add a 3-day forecast: call Open-Meteo's `daily` parameters for max/min temp
> and print a row per day. Keep all HTTP in `WeatherClient`, reuse the same
> `HttpClient`, and pass the `CancellationToken` through. Show the plan first."

**Refactor toward production:**
> "Show me how to convert this to use `IHttpClientFactory` and DI with a typed
> `WeatherClient`, including a Polly retry policy for transient errors. Explain
> each change and why it's better than `new HttpClient()`."

**Practise the review skill:**
> "Review `WeatherClient` for `HttpClient` and async pitfalls: socket reuse,
> missing `ConfigureAwait`, cancellation, and error handling. Answer as a
> checklist before writing any code."

**Turn it into a service:**
> "Wrap this in a Minimal API endpoint `GET /weather?city=...` that returns JSON.
> Reuse `WeatherClient` unchanged and register it with `IHttpClientFactory`."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and make sure you can explain every `await` you accept.

---

## Next

That's the **Starter** tier done 🎉 — up next is the **Intermediate** tier:
URL Shortener, Blog API with Auth, and a Background Job Runner.
