using System;
using System.Collections.Generic;

namespace LegacyShop.Data.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }

        // TRAP T26: required relationship -> EF6 cascade delete by convention.
        // EF Core also cascades here, but deletes orphans on save; EF6 throws instead.
        public virtual ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }
}
