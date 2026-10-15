using System.Security.Claims;
using System.Text;
using BlogApi;
using BlogApi.Auth;
using BlogApi.Data;
using BlogApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// =============================================================================
// Blog API with Auth — Intermediate project #5
// Focus: JWT authentication, input validation, and integration tests.
//
//   POST /auth/register     create an account
//   POST /auth/login        get a JWT
//   GET  /posts             list (public)
//   POST /posts             create (auth required)
//   PUT  /posts/{id}        update (author only)
//   DELETE /posts/{id}      delete (author only)
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlogDbContext>(o => o.UseSqlite("Data Source=blog.db"));

// --- JWT configuration -------------------------------------------------------
// Bind the "Jwt" section, with a DEV-ONLY fallback key so the sample runs out of
// the box. NEVER ship a hard-coded key: in production set Jwt:Key via user-secrets,
// environment variables, or a secret store. HmacSha256 needs a >= 32-byte key.
var jwt = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwt);
if (string.IsNullOrWhiteSpace(jwt.Key))
    jwt.Key = "dev-only-super-secret-key-change-me-please-32b";

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep original claim names ("sub" stays "sub" instead of being remapped).
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,   // the important one: verify our signature
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BlogDbContext>().Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// --- Auth endpoints ----------------------------------------------------------

app.MapPost("/auth/register", async (RegisterRequest request, BlogDbContext db) =>
{
    if (RequestValidator.Validate(request) is { } error)
        return error;

    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.Conflict(new { error = "Username is already taken." });

    var user = new User
    {
        Username = request.Username,
        PasswordHash = PasswordHasher.Hash(request.Password) // never store the raw password
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new { user.Id, user.Username });
});

app.MapPost("/auth/login", async (LoginRequest request, BlogDbContext db, JwtTokenService tokens) =>
{
    if (RequestValidator.Validate(request) is { } error)
        return error;

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

    // Same 401 whether the username or the password is wrong — don't reveal which.
    if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok(new { token = tokens.CreateToken(user) });
});

// --- Post endpoints ----------------------------------------------------------

var posts = app.MapGroup("/posts");

// Public reads.
posts.MapGet("/", async (BlogDbContext db) =>
    await db.Posts
            .OrderByDescending(p => p.Id)
            .Select(p => new { p.Id, p.Title, p.Content, p.CreatedAt, Author = p.Author!.Username })
            .ToListAsync());

posts.MapGet("/{id:int}", async (int id, BlogDbContext db) =>
    await db.Posts
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Title, p.Content, p.CreatedAt, Author = p.Author!.Username })
            .FirstOrDefaultAsync() is { } post
        ? Results.Ok(post)
        : Results.NotFound());

// Protected writes — .RequireAuthorization() rejects missing/invalid tokens with 401.
posts.MapPost("/", async (CreatePostRequest request, ClaimsPrincipal principal, BlogDbContext db) =>
{
    if (RequestValidator.Validate(request) is { } error)
        return error;

    var authorId = GetUserId(principal);
    var post = new BlogPost { Title = request.Title, Content = request.Content, AuthorId = authorId };

    db.Posts.Add(post);
    await db.SaveChangesAsync();

    return Results.Created($"/posts/{post.Id}", new { post.Id, post.Title, post.Content, post.CreatedAt });
})
.RequireAuthorization();

posts.MapPut("/{id:int}", async (int id, UpdatePostRequest request, ClaimsPrincipal principal, BlogDbContext db) =>
{
    if (RequestValidator.Validate(request) is { } error)
        return error;

    var post = await db.Posts.FindAsync(id);
    if (post is null)
        return Results.NotFound();

    // Ownership check: you can only edit YOUR OWN posts.
    if (post.AuthorId != GetUserId(principal))
        return Results.Forbid();

    post.Title = request.Title;
    post.Content = request.Content;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

posts.MapDelete("/{id:int}", async (int id, ClaimsPrincipal principal, BlogDbContext db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null)
        return Results.NotFound();

    if (post.AuthorId != GetUserId(principal))
        return Results.Forbid();

    db.Posts.Remove(post);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "Blog API with Auth", docs = "see README.md" }));

app.Run();

// Reads the user id from the "sub" claim of the validated JWT.
static int GetUserId(ClaimsPrincipal principal) =>
    int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
              ?? throw new InvalidOperationException("Token has no subject claim."));

// Exposes the implicit Program class so the test project can spin up the app
// with WebApplicationFactory<Program>.
public partial class Program;
