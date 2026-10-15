using System.ComponentModel.DataAnnotations;

namespace BlogApi.Models;

/// <summary>An application user. We store only a password HASH, never the password.</summary>
public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }

    /// <summary>PBKDF2 hash (salt + derived key), produced by <c>PasswordHasher</c>.</summary>
    public required string PasswordHash { get; set; }

    public ICollection<BlogPost> Posts { get; set; } = [];
}

/// <summary>A blog post, owned by the <see cref="User"/> who created it.</summary>
public class BlogPost
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Foreign key back to the author. Only the author may edit/delete their post.
    public int AuthorId { get; set; }
    public User? Author { get; set; }
}

// --- DTOs: the ONLY shapes a client is allowed to send ----------------------

public record RegisterRequest(
    [property: Required, MinLength(3), MaxLength(50)] string Username,
    [property: Required, MinLength(8), MaxLength(100)] string Password);

public record LoginRequest(
    [property: Required] string Username,
    [property: Required] string Password);

public record CreatePostRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MinLength(1)] string Content);

public record UpdatePostRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MinLength(1)] string Content);
