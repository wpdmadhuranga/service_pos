using backend.Domain.Entities.Inventory;

namespace backend.Domain.Entities.Inventory
{
    public class ServiceInventoryMapping
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public Guid InventoryItemId { get; set; }
        public decimal DefaultQuantity { get; set; } 
        
        public Service Service { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}