using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyShop.Core
{
    [Serializable]
    public class CartLine
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class PriceBreakdown
    {
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Pure business logic - the part that should move first and get characterization tests.
    /// It has a few quirks on purpose (rounding, the "3 for 2" rule on stickers) that a rewrite
    /// would quietly "fix" and change the totals customers see.
    /// </summary>
    public class PriceCalculator
    {
        private readonly decimal _vatRate;
        private readonly decimal _freeShippingFrom;

        public PriceCalculator() : this(AppSettings.VatRate, AppSettings.FreeShippingFrom) { }

        public PriceCalculator(decimal vatRate, decimal freeShippingFrom)
        {
            _vatRate = vatRate;
            _freeShippingFrom = freeShippingFrom;
        }

        public PriceBreakdown Calculate(IList<CartLine> lines, string couponCode)
        {
            var result = new PriceBreakdown();
            if (lines == null || lines.Count == 0) return result;

            result.Subtotal = lines.Sum(l => l.UnitPrice * l.Quantity);

            // QUIRK: "3 for 2" on any product whose name starts with "Sticker", counted per line, not per cart.
            foreach (var line in lines.Where(l => l.Name != null && l.Name.StartsWith("Sticker")))
            {
                result.Discount += (line.Quantity / 3) * line.UnitPrice;
            }

            // QUIRK: coupon is case-insensitive and applied AFTER the sticker discount, rounded DOWN to cents.
            if (!String.IsNullOrEmpty(couponCode) && couponCode.Trim().ToUpperInvariant() == "LEGACY10")
            {
                var afterStickers = result.Subtotal - result.Discount;
                result.Discount += Math.Floor(afterStickers * 0.10m * 100m) / 100m;
            }

            var net = result.Subtotal - result.Discount;
            result.Shipping = net >= _freeShippingFrom ? 0m : 4.90m;

            // QUIRK: VAT is calculated on goods only, shipping is VAT-free, and rounding is banker's rounding (default).
            result.Vat = Math.Round(net * _vatRate, 2);
            result.Total = net + result.Shipping + result.Vat;
            return result;
        }
    }
}
