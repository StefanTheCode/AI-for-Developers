using System;
using System.Linq;
using System.ServiceModel;
using LegacyShop.Data;

namespace LegacyShop.Inventory
{
    public class InventoryService : IInventoryService
    {
        private const int LowStockThreshold = 5;

        public StockInfo GetStock(int productId)
        {
            using (var db = new ShopContext())
            {
                var product = db.Products.FirstOrDefault(p => p.Id == productId);
                var available = product == null ? 0 : product.Stock;
                return new StockInfo { ProductId = productId, Available = available, LowStock = available < LowStockThreshold };
            }
        }

        public ReservationResult Reserve(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new FaultException<InventoryFault>(
                    new InventoryFault { Code = "INVALID_QUANTITY", Message = "Quantity must be positive." },
                    new FaultReason("Invalid quantity"));
            }

            using (var db = new ShopContext())
            {
                var product = db.Products.FirstOrDefault(p => p.Id == productId);
                if (product == null || product.Stock < quantity)
                {
                    throw new FaultException<InventoryFault>(
                        new InventoryFault { Code = "OUT_OF_STOCK", Message = "Not enough stock for product " + productId + "." },
                        new FaultReason("Out of stock"));
                }

                product.Stock -= quantity;
                db.SaveChanges();
                return new ReservationResult { ReservationId = Guid.NewGuid().ToString("N"), Reserved = quantity };
            }
        }

        public void Release(string reservationId)
        {
            // Not implemented in 2013 either. Kept so the contract stays the same.
        }
    }
}
