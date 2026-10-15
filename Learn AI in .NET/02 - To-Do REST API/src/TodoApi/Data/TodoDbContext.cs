using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>
/// The EF Core <see cref="DbContext"/> — your gateway to the database.
///
/// LESSON — a DbContext is a "unit of work".
/// Each <see cref="DbSet{TEntity}"/> is a table you can query with LINQ. EF Core
/// translates that LINQ into SQL, tracks the objects you load, and writes your
/// changes back when you call <see cref="DbContext.SaveChangesAsync"/>.
///
/// It's registered in DI (see Program.cs) with a "scoped" lifetime — one context
/// per HTTP request — which is exactly what you want for a web API.
/// </summary>
public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent configuration lives here. For a starter app the conventions are
        // enough, but this is where you'd add indexes, constraints, etc.
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
        });
    }
}
