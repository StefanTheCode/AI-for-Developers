using System.Collections.Generic;
using System.Linq;
using System.Web;
using LegacyShop.Core;

namespace LegacyShop.Web.Infrastructure
{
    /// <summary>
    /// TRAP T04: the cart is a List of objects in InProc session state.
    /// ASP.NET Core session stores byte[] / strings only, and is not shared with the old app
    /// unless you use the System.Web adapters' remote session.
    /// </summary>
    public static class CartSession
    {
        private const string Key = "Cart";

        public static List<CartLine> Lines
        {
            get
            {
                var session = HttpContext.Current.Session;
                var lines = session[Key] as List<CartLine>;
                if (lines == null)
                {
                    lines = new List<CartLine>();
                    session[Key] = lines;
                }
                return lines;
            }
        }

        public static int Count
        {
            get { return Lines.Sum(l => l.Quantity); }
        }

        public static void Add(int productId, string name, decimal unitPrice, int quantity)
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == productId);
            if (existing != null)
            {
                // QUIRK: keeps the FIRST price it saw, even if the product price changed since.
                existing.Quantity += quantity;
                return;
            }
            Lines.Add(new CartLine { ProductId = productId, Name = name, UnitPrice = unitPrice, Quantity = quantity });
        }

        public static void SetQuantity(int productId, int quantity)
        {
            var line = Lines.FirstOrDefault(l => l.ProductId == productId);
            if (line == null) return;
            if (quantity <= 0) Lines.Remove(line);
            else line.Quantity = quantity;
        }

        public static void Remove(int productId)
        {
            Lines.RemoveAll(l => l.ProductId == productId);
        }

        public static void Clear()
        {
            Lines.Clear();
        }
    }
}
