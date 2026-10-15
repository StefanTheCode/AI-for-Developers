using System.ComponentModel;

namespace InvoiceParser.Models;

/// <summary>
/// The structured shape we want the LLM to extract an invoice into.
///
/// LESSON — the type IS the prompt (structured output).
/// Microsoft.Extensions.AI turns this class into a JSON schema and asks the model
/// to return data that matches it. The <see cref="DescriptionAttribute"/>s become
/// hints in that schema, steering the extraction. We use plain get/set properties
/// (not positional records) because the schema binder fills them in.
/// </summary>
public sealed class Invoice
{
    [Description("The invoice number or ID, e.g. INV-1042")]
    public string? InvoiceNumber { get; set; }

    [Description("The vendor / seller company name")]
    public string? VendorName { get; set; }

    [Description("Invoice date in ISO format YYYY-MM-DD")]
    public string? InvoiceDate { get; set; }

    [Description("3-letter currency code, e.g. USD, EUR")]
    public string? Currency { get; set; }

    public List<InvoiceLine> LineItems { get; set; } = [];

    [Description("Sum of all line totals, before tax")]
    public decimal Subtotal { get; set; }

    [Description("Total tax amount")]
    public decimal Tax { get; set; }

    [Description("Grand total the customer must pay (subtotal + tax)")]
    public decimal Total { get; set; }
}

/// <summary>One line on the invoice.</summary>
public sealed class InvoiceLine
{
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [Description("Quantity multiplied by unit price")]
    public decimal LineTotal { get; set; }
}
