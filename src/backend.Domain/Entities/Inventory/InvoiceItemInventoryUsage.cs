using backend.Domain.Entities;

namespace backend.Domain.Entities.Inventory
{
    public class InvoiceItemInventoryUsage
    {
        public Guid Id { get; set; }
        public Guid InvoiceItemId { get; set; }
        public Guid InventoryItemId { get; set; }
        public decimal QuantityUsed { get; set; } // may differ from the default recipe

        // Navigation
        public InvoiceItem InvoiceItem { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}