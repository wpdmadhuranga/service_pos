using backend.Domain.Entities;

namespace backend.Application.DTOs.Inventory.Products
{
    
            public class ProductDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PartNumber { get; set; }
        public string? CompatibleVehicleType { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; }
        public string? InventoryItemId { get; set; }
    }



}