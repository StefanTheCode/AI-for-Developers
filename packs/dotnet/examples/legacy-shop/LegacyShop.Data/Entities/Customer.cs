using System.Collections.Generic;

namespace LegacyShop.Data.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }

        // Base64 SHA-256 of salt + password. Not great, but very typical for a 2012 app.
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }

        // "Admin" or "Customer" - put into the Forms auth ticket (TRAP T17).
        public string Role { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
