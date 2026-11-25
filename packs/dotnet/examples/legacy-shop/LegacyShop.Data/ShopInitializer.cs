using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using LegacyShop.Data.Entities;

namespace LegacyShop.Data
{
    // TRAP T26: database initializers + Seed do not exist in EF Core (use migrations + HasData / seeding).
    public class ShopInitializer : DropCreateDatabaseIfModelChanges<ShopContext>
    {
        protected override void Seed(ShopContext context)
        {
            var books = new Category { Name = "Books" };
            var gear = new Category { Name = "Gear" };
            var courses = new Category { Name = "Courses" };
            context.Categories.AddRange(new[] { books, gear, courses });

            var products = new List<Product>
            {
                new Product { Name = "Design Patterns Simplified", Description = "Ebook, 120 pages.", Price = 19.99m, Stock = 999, IsActive = true, Category = books },
                new Product { Name = "Pragmatic .NET Code Rules", Description = "Video course.", Price = 49.00m, Stock = 999, IsActive = true, Category = courses },
                new Product { Name = "Mechanical Keyboard", Description = "Brown switches.", Price = 129.00m, Stock = 12, IsActive = true, Category = gear },
                new Product { Name = "USB-C Dock", Description = "Two 4K screens.", Price = 89.50m, Stock = 4, IsActive = true, Category = gear },
                new Product { Name = "Desk Mat", Description = "90 x 40 cm.", Price = 24.00m, Stock = 40, IsActive = true, Category = gear },
                new Product { Name = "C# in Depth (used)", Description = "Third edition.", Price = 15.00m, Stock = 1, IsActive = true, Category = books },
                new Product { Name = "Old Laptop Stand", Description = "Discontinued.", Price = 10.00m, Stock = 0, IsActive = false, Category = gear },
            };
            for (var i = 1; i <= 18; i++)
            {
                products.Add(new Product { Name = "Sticker #" + i, Description = "Vinyl sticker.", Price = 2.50m, Stock = 100, IsActive = true, Category = gear });
            }
            context.Products.AddRange(products);

            context.Customers.Add(CreateCustomer("admin@legacyshop.local", "Admin", "admin123", "Admin"));
            context.Customers.Add(CreateCustomer("ana@legacyshop.local", "Ana", "ana123", "Customer"));

            context.SaveChanges();
        }

        public static Customer CreateCustomer(string email, string name, string password, string role)
        {
            var salt = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return new Customer
            {
                Email = email,
                DisplayName = name,
                PasswordSalt = salt,
                PasswordHash = Hash(password, salt),
                Role = role
            };
        }

        public static string Hash(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
            }
        }
    }
}
