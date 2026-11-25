using System.Collections.Generic;

namespace LegacyShop.Data.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // TRAP T25: virtual navigation = EF6 lazy loading. EF Core does not lazy load by default.
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
