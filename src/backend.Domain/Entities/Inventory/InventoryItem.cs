
namespace backend.Domain.Entities.Inventory
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. "5W-30 Engine Oil"
        public string? Sku { get; set; }
        public string Unit { get; set; } = string.Empty; // litre / piece / kg
        public decimal QuantityOnHand { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; } // what you pay - separate from what you charge
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<ServiceInventoryMapping> ServiceMappings { get; set; } = new List<ServiceInventoryMapping>();
        public ICollection<InvoiceItemInventoryUsage> UsageRecords { get; set; } = new List<InvoiceItemInventoryUsage>();
    }
}