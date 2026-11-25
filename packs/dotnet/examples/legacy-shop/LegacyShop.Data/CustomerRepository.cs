using System.Linq;
using LegacyShop.Data.Entities;

namespace LegacyShop.Data
{
    public class CustomerRepository
    {
        public Customer Validate(string email, string password)
        {
            using (var db = new ShopContext())
            {
                var customer = db.Customers.FirstOrDefault(c => c.Email == email);
                if (customer == null) return null;
                return ShopInitializer.Hash(password, customer.PasswordSalt) == customer.PasswordHash ? customer : null;
            }
        }

        public Customer Find(int id)
        {
            using (var db = new ShopContext())
            {
                return db.Customers.Find(id);
            }
        }
    }
}
