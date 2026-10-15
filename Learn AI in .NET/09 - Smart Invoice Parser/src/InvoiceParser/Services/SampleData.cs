using InvoiceParser.Models;

namespace InvoiceParser.Services;

/// <summary>Raw sample invoices (as a human would paste them) plus one canned result.</summary>
public static class SampleData
{
    /// <summary>A clean, internally-consistent invoice.</summary>
    public const string CleanInvoice = """
        ACME Tools LLC
        Invoice #INV-1042
        Date: 2026-03-14
        Bill to: Contoso Cloud

        Qty  Description            Unit Price   Amount
        2    Hex wrench set         12.50        25.00
        1    Cordless drill         89.99        89.99
        3    Safety goggles          7.00        21.00

        Subtotal: 135.99
        Tax (10%): 13.60
        Total: 149.59
        Currency: USD
        """;

    /// <summary>
    /// A messy invoice whose own numbers DON'T add up (365.00 + 73.00 = 438.00,
    /// not 448.00). If the model extracts faithfully, the validator will flag it.
    /// </summary>
    public const string InconsistentInvoice = """
        Globex Supplies — INVOICE 7781
        14 Apr 2026
        1 x Server rack @ 320.00 = 320.00
        10 x Cat6 cable @ 4.50 = 45.00
        Subtotal 365.00
        VAT 20% 73.00
        TOTAL 448.00
        Currency: EUR
        """;

    /// <summary>
    /// A pre-extracted invoice with an injected math error, used for the OFFLINE
    /// demo so the validator lesson works even without Ollama running. The line
    /// total (2 × 12.50 = 25.00) is wrong on purpose (says 30.00), and the grand
    /// total doesn't match either.
    /// </summary>
    public static Invoice CannedBadExtraction() => new()
    {
        InvoiceNumber = "INV-9001",
        VendorName = "Initech",
        InvoiceDate = "2026-05-02",
        Currency = "USD",
        LineItems =
        [
            new InvoiceLine { Description = "Stapler", Quantity = 2, UnitPrice = 12.50m, LineTotal = 30.00m },
            new InvoiceLine { Description = "Paper (ream)", Quantity = 5, UnitPrice = 4.00m, LineTotal = 20.00m }
        ],
        Subtotal = 50.00m,
        Tax = 5.00m,
        Total = 60.00m
    };
}
