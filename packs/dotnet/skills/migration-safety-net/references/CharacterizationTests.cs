// Starting point for characterization (golden master) tests of business logic.
// Step 1: run with GOLDEN_MODE=record to write golden/price-calculator.json from the CURRENT code.
// Step 2: commit it. From then on the test compares every case with the golden file.
// Target net48 while the library is net48 (Windows), add net10.0 when the library multi-targets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace CharacterizationTests
{
    public class PriceCalculatorCharacterization
    {
        private static readonly string Golden = Path.Combine(AppContext.BaseDirectory, "golden", "price-calculator.json");
        private static readonly bool Record = Environment.GetEnvironmentVariable("GOLDEN_MODE") == "record";

        [Fact]
        public void Matches_the_golden_master()
        {
            var calculator = new LegacyShop.Core.PriceCalculator(0.20m, 100m);   // fixed config, no Web.config
            var results = Cases().Select(c => new
            {
                c.Name,
                Result = calculator.Calculate(c.Lines, c.Coupon)
            }).ToList();

            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });

            if (Record)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Golden)!);
                File.WriteAllText(Golden, json);
                return;
            }

            Assert.Equal(File.ReadAllText(Golden), json);
        }

        private static IEnumerable<(string Name, List<LegacyShop.Core.CartLine> Lines, string Coupon)> Cases()
        {
            LegacyShop.Core.CartLine L(string name, decimal price, int qty) =>
                new LegacyShop.Core.CartLine { ProductId = name.GetHashCode(), Name = name, UnitPrice = price, Quantity = qty };

            yield return ("empty cart", new List<LegacyShop.Core.CartLine>(), null);
            yield return ("one item under free shipping", new() { L("Desk Mat", 24.00m, 1) }, null);
            yield return ("exactly at free shipping", new() { L("Desk Mat", 25.00m, 4) }, null);
            // QUIRK: 3-for-2 per LINE - two lines of 2 stickers get no discount
            yield return ("stickers 3 on one line", new() { L("Sticker #1", 2.50m, 3) }, null);
            yield return ("stickers 2 + 2 on two lines", new() { L("Sticker #1", 2.50m, 2), L("Sticker #2", 2.50m, 2) }, null);
            // QUIRK: coupon is case-insensitive, trimmed, applied after stickers, rounded DOWN
            yield return ("coupon lowercase with spaces", new() { L("USB-C Dock", 89.50m, 1) }, "  legacy10 ");
            yield return ("coupon rounding down", new() { L("Odd", 33.33m, 1) }, "LEGACY10");
            // QUIRK: banker's rounding on VAT
            yield return ("vat midpoint", new() { L("Midpoint", 0.125m, 1) }, null);
            yield return ("unknown coupon", new() { L("Desk Mat", 24.00m, 1) }, "SAVE50");
            yield return ("big order", new() { L("Mechanical Keyboard", 129.00m, 3), L("Sticker #9", 2.50m, 7) }, "LEGACY10");
        }
    }
}
