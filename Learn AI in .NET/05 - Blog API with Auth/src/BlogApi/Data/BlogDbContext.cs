using Microsoft.EntityFrameworkCore;
using BlogApi.Models;

namespace BlogApi.Data;

/// <summary>EF Core context for users and posts.</summary>
public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BlogPost> Posts => Set<BlogPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            // Usernames must be unique — enforced at the database level, not just in code.
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(p => p.Author)
                  .WithMany(u => u.Posts)
                  .HasForeignKey(p => p.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
