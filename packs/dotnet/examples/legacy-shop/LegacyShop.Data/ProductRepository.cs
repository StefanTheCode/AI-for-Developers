using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LegacyShop.Data.Entities;

namespace LegacyShop.Data
{
    public class ProductRepository
    {
        public List<Product> GetActive(int? categoryId, string sortBy)
        {
            using (var db = new ShopContext())
            {
                var query = db.Products.Where(p => p.IsActive);
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                switch (sortBy)
                {
                    case "Price": query = query.OrderBy(p => p.Price); break;
                    case "Price DESC": query = query.OrderByDescending(p => p.Price); break;
                    default: query = query.OrderBy(p => p.Name); break;
                }

                // TRAP T27: string-based Include. EF Core prefers the lambda form; the string form still exists but behaves differently on typos.
                return query.Include("Category").ToList();
            }
        }

        public Product Find(int id)
        {
            using (var db = new ShopContext())
            {
                return db.Products.Include("Category").FirstOrDefault(p => p.Id == id);
            }
        }

        public List<Category> GetCategories()
        {
            using (var db = new ShopContext())
            {
                return db.Categories.OrderBy(c => c.Name).ToList();
            }
        }
    }
}
