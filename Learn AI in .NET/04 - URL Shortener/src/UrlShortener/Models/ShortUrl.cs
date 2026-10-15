using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models;

/// <summary>
/// One shortened link — the EF Core entity (one row in "ShortUrls").
///
/// LESSON — index the column you look up by.
/// Every redirect searches by <see cref="Code"/>, so it gets a UNIQUE index
/// (configured in the DbContext). Without it, each lookup is a full table scan.
/// </summary>
public class ShortUrl
{
    public int Id { get; set; }

    /// <summary>The short code, e.g. "aB3xK9q". Unique.</summary>
    public required string Code { get; set; }

    /// <summary>The full destination we redirect to.</summary>
    public required string OriginalUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>How many times the short link has been followed.</summary>
    public long Hits { get; set; }
}

/// <summary>What a client sends to create a short link.</summary>
public record ShortenRequest(
    [property: Required, Url, MaxLength(2048)] string Url);
