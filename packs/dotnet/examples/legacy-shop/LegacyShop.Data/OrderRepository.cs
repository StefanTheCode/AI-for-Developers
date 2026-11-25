using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LegacyShop.Data.Entities;

namespace LegacyShop.Data
{
    public class SalesRow
    {
        public DateTime Day { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderRepository
    {
        public Order Create(int customerId, IEnumerable<OrderLine> lines)
        {
            using (var db = new ShopContext())
            {
                var order = new Order { CustomerId = customerId, CreatedAt = DateTime.Now, Status = "New" };
                foreach (var line in lines)
                {
                    order.Lines.Add(line);
                }
                order.Total = order.Lines.Sum(l => l.UnitPrice * l.Quantity);
                db.Orders.Add(order);
                db.SaveChanges();
                return order;
            }
        }

        public List<Order> ForCustomer(int customerId)
        {
            // TRAP T25: the context is NOT disposed here on purpose (old "repository" style), so the
            // caller can touch order.Lines / line.Product later -> lazy loading, N+1 queries.
            var db = new ShopContext();
            return db.Orders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt).ToList();
        }

        public List<SalesRow> SalesByDay(int days)
        {
            using (var db = new ShopContext())
            {
                var from = DateTime.Today.AddDays(-days);
                // TRAP T27: DbFunctions.TruncateTime is EF6-only. EF Core translates o.CreatedAt.Date instead.
                return db.Orders
                    .Where(o => o.CreatedAt >= from)
                    .GroupBy(o => DbFunctions.TruncateTime(o.CreatedAt))
                    .Select(g => new SalesRow { Day = g.Key.Value, Orders = g.Count(), Revenue = g.Sum(o => o.Total) })
                    .OrderBy(r => r.Day)
                    .ToList();
            }
        }

        public List<KeyValuePair<string, int>> TopProducts(int take)
        {
            using (var db = new ShopContext())
            {
                return db.OrderLines
                    .GroupBy(l => l.Product.Name)
                    .Select(g => new { Name = g.Key, Qty = g.Sum(l => l.Quantity) })
                    .OrderByDescending(x => x.Qty)
                    .Take(take)
                    .ToList()
                    .Select(x => new KeyValuePair<string, int>(x.Name, x.Qty))
                    .ToList();
            }
        }

        public void Delete(int orderId)
        {
            using (var db = new ShopContext())
            {
                var order = db.Orders.Find(orderId);
                if (order == null) return;
                db.Orders.Remove(order); // cascades to Lines (T26)
                db.SaveChanges();
            }
        }
    }
}
