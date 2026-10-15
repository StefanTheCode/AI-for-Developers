using BlogApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlogApi.Tests;

/// <summary>
/// Boots the REAL Blog API in memory for integration tests, but swaps its SQLite
/// file database for an in-memory SQLite connection so tests are fast and isolated.
///
/// LESSON — test the app as a black box.
/// WebApplicationFactory runs the actual pipeline (routing, auth, validation, EF).
/// These tests would catch a broken JWT config or a missing [Authorize] — things
/// pure unit tests never see.
/// </summary>
public sealed class BlogApiFactory : WebApplicationFactory<Program>
{
    // A single open connection keeps the in-memory database alive for the whole
    // test class. Close it and the data vanishes.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // Remove the app's real DbContext registration...
            services.RemoveAll<DbContextOptions<BlogDbContext>>();

            // ...and point it at our in-memory connection instead.
            services.AddDbContext<BlogDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
