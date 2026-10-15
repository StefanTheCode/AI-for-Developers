using System.ComponentModel.DataAnnotations;

namespace UrlShortener;

/// <summary>
/// Boundary validation using the framework's own <see cref="Validator"/> — no
/// extra packages. Returns a 400 <see cref="IResult"/> when invalid, else null.
/// </summary>
public static class RequestValidator
{
    public static IResult? Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(model, context, results, validateAllProperties: true))
            return null;

        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                        (r, member) => (Member: member, r.ErrorMessage))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid").ToArray());

        return Results.ValidationProblem(errors);
    }
}
