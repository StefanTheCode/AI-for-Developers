// Starting point for flow parity tests with Playwright (xUnit, net10.0).
// The same test runs against the old app and the new one - switch PARITY_BASE_URL.
// Drive the UI by what a user sees (labels, roles, button text), never by WebForms ids.
using Microsoft.Playwright;
using Xunit;

namespace ParityTests;

public class CartFlowTests : IAsyncLifetime
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("PARITY_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task Add_two_items_and_apply_coupon_shows_the_same_totals()
    {
        var page = await _browser.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Product.aspx?id=3");
        await page.GetByRole(AriaRole.Textbox).First.FillAsync("2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Added 2 to your cart.")).ToBeVisibleAsync();

        await page.GotoAsync($"{BaseUrl}/Cart.aspx");
        await page.GetByRole(AriaRole.Textbox).Last.FillAsync("legacy10");      // QUIRK: coupon is case-insensitive
        await page.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        // Read the totals table by its row labels, not by ids.
        var total = await RowValue(page, "Total");
        var vat = await RowValue(page, "VAT");
        var shipping = await RowValue(page, "Shipping");

        // Golden values recorded from the OLD app. Change them only with an accepted-differences entry.
        Assert.Equal("0.00", shipping);    // 2 x 129.00 - 10% = 232.20 -> free shipping
        Assert.Equal("46.44", vat);        // QUIRK: VAT on goods only, banker's rounding
        Assert.Equal("278.64", total);
    }

    private static async Task<string> RowValue(IPage page, string label)
    {
        var row = page.Locator("tr", new() { Has = page.GetByText(label, new() { Exact = true }) });
        return (await row.Locator("td").Last.InnerTextAsync()).Trim();
    }
}
