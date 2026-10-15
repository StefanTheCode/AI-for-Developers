using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

/// <summary>
/// A to-do item — this is our EF Core *entity*. Each instance maps to one row
/// in the "TodoItems" table.
///
/// LESSON — entities vs. DTOs.
/// This class is shaped for the DATABASE. We deliberately do NOT let clients
/// send us an `Id` or `CreatedAt` — those are owned by the server. That's why
/// the API accepts <see cref="CreateTodoRequest"/> / <see cref="UpdateTodoRequest"/>
/// (small "DTOs") instead of this entity. Mixing the two is a classic AI-code
/// smell that leads to over-posting bugs (a client setting fields it shouldn't).
/// </summary>
public class TodoItem
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public bool IsDone { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>What a client is allowed to send when CREATING a to-do.</summary>
/// <remarks>Data-annotation attributes drive automatic validation in the endpoint.</remarks>
public record CreateTodoRequest(
    [property: Required, MaxLength(200)] string Title);

/// <summary>What a client is allowed to send when UPDATING a to-do.</summary>
public record UpdateTodoRequest(
    [property: Required, MaxLength(200)] string Title,
    bool IsDone);
