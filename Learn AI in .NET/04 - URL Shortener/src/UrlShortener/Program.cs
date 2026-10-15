using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using UrlShortener;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortener.Services;

// =============================================================================
// URL Shortener — Intermediate project #4
// Focus: Caching + Rate limiting.
//
//   POST /shorten   { "url": "https://..." }  -> creates a short code (rate limited)
//   GET  /{code}                              -> 302 redirect (served from cache)
//   GET  /api/stats/{code}                    -> hit counter + metadata
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// --- Services ----------------------------------------------------------------

builder.Services.AddDbContext<UrlShortenerDbContext>(o =>
    o.UseSqlite("Data Source=urls.db"));

// In-memory cache that UrlShortenerService uses for the hot redirect path.
builder.Services.AddMemoryCache();

builder.Services.AddScoped<UrlShortenerService>();

// --- Rate limiting -----------------------------------------------------------
// LESSON — protect write endpoints from abuse.
// Creating links is cheap to spam. We add a per-IP fixed-window limiter: at most
// 5 "shorten" calls every 10 seconds. Over the limit -> HTTP 429. Redirects are
// intentionally NOT limited so real traffic keeps flowing.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("shorten", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Partition by client IP so one abuser can't starve everyone else.
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10)
            }));
});

var app = builder.Build();

// Create + seed the database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
    db.Database.EnsureCreated();
}

app.UseRateLimiter();

// --- Endpoints ---------------------------------------------------------------

// Create a short link. The .RequireRateLimiting("shorten") wires up the limiter.
app.MapPost("/shorten", async (ShortenRequest request, UrlShortenerService service, HttpRequest http, CancellationToken ct) =>
{
    if (RequestValidator.Validate(request) is { } validationError)
        return validationError;

    try
    {
        var created = await service.CreateAsync(request.Url, ct);
        var shortUrl = $"{http.Scheme}://{http.Host}/{created.Code}";

        return Results.Created(shortUrl, new
        {
            code = created.Code,
            shortUrl,
            originalUrl = created.OriginalUrl
        });
    }
    catch (ArgumentException ex)
    {
        // Thrown by the service for non-http(s) URLs — a clean 400.
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireRateLimiting("shorten");

// Follow a short link. Cache-first resolve, then a 302 redirect.
app.MapGet("/{code}", async (string code, UrlShortenerService service, CancellationToken ct) =>
{
    var target = await service.ResolveAsync(code, ct);
    if (target is null)
        return Results.NotFound($"No short link '{code}'.");

    // Fire-and-forget-ish: record the hit but don't make the user wait on it.
    await service.RecordHitAsync(code, ct);

    return Results.Redirect(target);
});

// Stats for a code.
app.MapGet("/api/stats/{code}", async (string code, UrlShortenerService service, CancellationToken ct) =>
{
    var entity = await service.GetAsync(code, ct);
    return entity is null
        ? Results.NotFound()
        : Results.Ok(new { entity.Code, entity.OriginalUrl, entity.Hits, entity.CreatedAt });
});

app.MapGet("/", () => Results.Ok(new
{
    service = "URL Shortener",
    usage = new
    {
        create = "POST /shorten { \"url\": \"https://example.com\" }",
        follow = "GET /{code}",
        stats = "GET /api/stats/{code}"
    }
}));

app.Run();
