using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Services;

/// <summary>
/// The core service: create short links and resolve them back to the original URL.
///
/// LESSON — cache the hot read path.
/// A redirect happens far more often than a "create". Hitting the database on
/// every single redirect is wasteful, so we put a small <see cref="IMemoryCache"/>
/// in front: resolve once from the DB, then serve subsequent hits from memory.
/// This is the classic "cache-aside" pattern.
/// </summary>
public sealed class UrlShortenerService(
    UrlShortenerDbContext db,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Creates a short link. Rejects anything that isn't an absolute http/https URL
    /// (an <c>Uri</c> guard that also blocks <c>javascript:</c> and other schemes).
    /// </summary>
    public async Task<ShortUrl> CreateAsync(string originalUrl, CancellationToken ct = default)
    {
        if (!IsSafeHttpUrl(originalUrl))
            throw new ArgumentException("Only absolute http/https URLs are allowed.", nameof(originalUrl));

        // Retry a few times in the (very unlikely) event of a code collision.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = ShortCodeGenerator.Generate();

            var exists = await db.ShortUrls.AnyAsync(s => s.Code == code, ct);
            if (exists)
                continue;

            var shortUrl = new ShortUrl { Code = code, OriginalUrl = originalUrl };
            db.ShortUrls.Add(shortUrl);
            await db.SaveChangesAsync(ct);

            return shortUrl;
        }

        throw new InvalidOperationException("Could not generate a unique code. Try again.");
    }

    /// <summary>
    /// Resolves a code to its original URL, using the cache first.
    /// Returns null if the code doesn't exist.
    /// </summary>
    public async Task<string?> ResolveAsync(string code, CancellationToken ct = default)
    {
        // 1) Fast path: already in memory?
        if (cache.TryGetValue(CacheKey(code), out string? cached))
            return cached;

        // 2) Slow path: read from the database...
        var entity = await db.ShortUrls.FirstOrDefaultAsync(s => s.Code == code, ct);
        if (entity is null)
            return null;

        // ...and store it for next time.
        cache.Set(CacheKey(code), entity.OriginalUrl, CacheDuration);
        return entity.OriginalUrl;
    }

    /// <summary>
    /// Increments the hit counter. Kept separate from resolve so the cached
    /// redirect stays fast — stats can tolerate a direct DB update.
    /// </summary>
    public Task RecordHitAsync(string code, CancellationToken ct = default) =>
        db.ShortUrls
          .Where(s => s.Code == code)
          .ExecuteUpdateAsync(s => s.SetProperty(x => x.Hits, x => x.Hits + 1), ct);

    public Task<ShortUrl?> GetAsync(string code, CancellationToken ct = default) =>
        db.ShortUrls.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code, ct);

    private static string CacheKey(string code) => $"shorturl:{code}";

    private static bool IsSafeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
