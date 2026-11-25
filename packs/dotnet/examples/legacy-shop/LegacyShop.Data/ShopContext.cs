using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using LegacyShop.Data.Entities;

namespace LegacyShop.Data
{
    public class ShopContext : DbContext
    {
        // Connection string "ShopContext" comes from Web.config / app config (TRAP T13 style config).
        public ShopContext() : base("name=ShopContext")
        {
            // TRAP T25: lazy loading + proxies are ON by default in EF6.
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // TRAP T27: EF6 fluent API (DbModelBuilder, HasRequired/WithMany) has no 1:1 EF Core equivalent.
            modelBuilder.Entity<OrderLine>()
                .HasRequired(l => l.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.OrderId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            modelBuilder.Entity<OrderLine>().Property(l => l.UnitPrice).HasPrecision(18, 2);
        }
    }
}
