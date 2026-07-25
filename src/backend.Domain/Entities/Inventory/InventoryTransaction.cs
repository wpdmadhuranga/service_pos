using backend.Domain.Enums;
namespace backend.Domain.Entities.Inventory
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public Guid InventoryItemId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public decimal Quantity { get; set; }

        // Links a StockOut transaction back to the invoice line that caused it.
        public Guid? ReferenceInvoiceItemId { get; set; }

        public string? Note { get; set; } // e.g. "damaged, written off"
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}