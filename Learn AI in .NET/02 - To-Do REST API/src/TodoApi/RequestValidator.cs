using System.ComponentModel.DataAnnotations;

namespace TodoApi;

/// <summary>
/// A tiny validation helper built entirely on the framework's own
/// <see cref="Validator"/> — no extra NuGet packages.
///
/// LESSON — validate at the boundary.
/// The only place untrusted data enters the app is the request body. We check
/// it there, once, and return a clean 400 with the reasons. Everything past this
/// point can then trust the data. (See OWASP: never trust client input.)
/// </summary>
public static class RequestValidator
{
    /// <summary>
    /// Validates an object against its DataAnnotation attributes.
    /// Returns a minimal-API <see cref="IResult"/> (400 with errors) when invalid,
    /// or <c>null</c> when the object is valid.
    /// </summary>
    public static IResult? Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(model, context, results, validateAllProperties: true))
            return null; // valid

        // Shape the errors like ASP.NET Core's ValidationProblem output.
        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                        (r, member) => (Member: member, r.ErrorMessage))
            .GroupBy(x => x.Member)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid").ToArray());

        return Results.ValidationProblem(errors);
    }
}
