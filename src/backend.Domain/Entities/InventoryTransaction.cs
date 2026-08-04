using backend.Domain.Enums;

namespace backend.Domain.Entities
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public Guid? InvoiceId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
        public Invoice? Invoice { get; set; }
    }
}