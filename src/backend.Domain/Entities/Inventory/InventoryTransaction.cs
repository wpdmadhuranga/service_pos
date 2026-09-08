using backend.Domain.Enums;
namespace backend.Domain.Entities.Inventory
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public Guid InventoryItemId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public Guid? ReferenceInvoiceItemId { get; set; }
        public string? Note { get; set; } 
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}