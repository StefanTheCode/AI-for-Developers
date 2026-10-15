using InvoiceParser.Models;

namespace InvoiceParser.Services;

/// <summary>
/// Validates an extracted <see cref="Invoice"/> with deterministic rules.
///
/// LESSON — never trust the AI's numbers. Verify them.
/// An LLM can misread a digit, drop a line, or "helpfully" round. This validator
/// is plain C# math with ZERO AI: it checks that quantities × prices equal line
/// totals, that line totals sum to the subtotal, and that subtotal + tax equals
/// the grand total. If the model (or the source invoice) is wrong, we catch it.
///
/// It's a pure function — trivial to unit-test and impossible to hallucinate.
/// </summary>
public static class InvoiceValidator
{
    // Money comparisons need a tolerance for rounding (1 cent here).
    private const decimal Tolerance = 0.01m;

    public static IReadOnlyList<string> Validate(Invoice invoice)
    {
        var issues = new List<string>();

        // --- required fields ---
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            issues.Add("Missing invoice number.");

        if (string.IsNullOrWhiteSpace(invoice.VendorName))
            issues.Add("Missing vendor name.");

        if (!DateOnly.TryParse(invoice.InvoiceDate, out _))
            issues.Add($"Invoice date is missing or not a valid date: '{invoice.InvoiceDate}'.");

        if (invoice.LineItems.Count == 0)
            issues.Add("No line items were extracted.");

        // --- per-line math: quantity × unit price should equal the line total ---
        for (var i = 0; i < invoice.LineItems.Count; i++)
        {
            var line = invoice.LineItems[i];
            var expected = line.Quantity * line.UnitPrice;

            if (Math.Abs(expected - line.LineTotal) > Tolerance)
            {
                issues.Add(
                    $"Line {i + 1} '{line.Description}': {line.Quantity} × {line.UnitPrice} = {expected}, " +
                    $"but the line total says {line.LineTotal}.");
            }
        }

        // --- subtotal should equal the sum of the line totals ---
        var sumOfLines = invoice.LineItems.Sum(l => l.LineTotal);
        if (Math.Abs(sumOfLines - invoice.Subtotal) > Tolerance)
            issues.Add($"Subtotal is {invoice.Subtotal}, but the line totals add up to {sumOfLines}.");

        // --- subtotal + tax should equal the grand total ---
        var computedTotal = invoice.Subtotal + invoice.Tax;
        if (Math.Abs(computedTotal - invoice.Total) > Tolerance)
            issues.Add($"Subtotal {invoice.Subtotal} + tax {invoice.Tax} = {computedTotal}, " +
                       $"but the total says {invoice.Total}.");

        return issues;
    }
}
