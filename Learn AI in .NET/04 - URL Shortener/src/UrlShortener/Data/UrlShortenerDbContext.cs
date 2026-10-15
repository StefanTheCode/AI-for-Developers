using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Data;

/// <summary>EF Core context holding the short-URL table.</summary>
public class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options)
    : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.Property(s => s.Code).IsRequired().HasMaxLength(16);
            entity.Property(s => s.OriginalUrl).IsRequired().HasMaxLength(2048);

            // Unique index: fast lookups by code AND a guarantee no two rows
            // share a code, even under a race between two inserts.
            entity.HasIndex(s => s.Code).IsUnique();
        });
    }
}
