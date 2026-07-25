using backend.Domain.Entities.Inventory;

namespace backend.Domain.Entities.Inventory
{
    public class ServiceInventoryMapping
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public Guid InventoryItemId { get; set; }
        public decimal DefaultQuantity { get; set; } // e.g. "Engine oil (4L)" service -> 4 units of oil

        // Navigation
        public Service Service { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}