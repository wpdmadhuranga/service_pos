using System.ComponentModel.DataAnnotations;
using backend.Domain.Enums;

namespace backend.Application.DTOs.Inventory
{
    public sealed record ProductDto(
        Guid Id,
        Guid? ServiceId,
        Guid? InventoryItemId,
        string Brand,
        string Name,
        string? PartNumber,
        string? CompatibleVehicleType,
        decimal CostPrice,
        decimal SellingPrice,
        int StockQuantity,
        string? Unit,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record ProductSummaryDto(
        Guid Id,
        string Brand,
        string Name,
        decimal SellingPrice,
        int StockQuantity);

    public sealed record ProductCreateRequest
    {
        public Guid? ServiceId { get; init; }

        [Required]
        [StringLength(100)]
        public string Brand { get; init; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; init; } = string.Empty;

        [StringLength(60)]
        public string? PartNumber { get; init; }

        [StringLength(60)]
        public string? CompatibleVehicleType { get; init; }

        public decimal CostPrice { get; init; }

        public decimal SellingPrice { get; init; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; init; }

        [StringLength(20)]
        public string? Unit { get; init; }

        public bool IsActive { get; init; } = true;
    }

    public sealed record ProductUpdateRequest
    {
        public Guid? ServiceId { get; init; }

        [StringLength(100)]
        public string? Brand { get; init; }

        [StringLength(150)]
        public string? Name { get; init; }

        [StringLength(60)]
        public string? PartNumber { get; init; }

        [StringLength(60)]
        public string? CompatibleVehicleType { get; init; }
        public decimal? CostPrice { get; init; }
        public decimal? SellingPrice { get; init; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; init; }

        [StringLength(20)]
        public string? Unit { get; init; }

        public bool? IsActive { get; init; }
    }

    public sealed record StockInRequest
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; init; }

        [StringLength(500)]
        public string? Notes { get; init; }
    }
    public class CreateInventoryItemDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(60)]
        public string? Sku { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        public decimal QuantityOnHand { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }

        public Guid? LinkToExistingProductId { get; set; }
        public bool CreateAsProduct { get; set; } = false;

        [StringLength(100)]
        public string? ProductBrand { get; set; }

        [StringLength(60)]
        public string? ProductPartNumber { get; set; }

        [StringLength(60)]
        public string? ProductCompatibleVehicleType { get; set; }

        public decimal? ProductSellingPrice { get; set; }

        public Guid? ProductServiceId { get; set; }
    }

    public class StockAdjustmentDto
    {
        [Range(typeof(decimal), "0.00000001", "79228162514264337593543950335")]
        public decimal Quantity { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
        public Guid UserId { get; set; }
    }

    public class InventoryItemResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        public bool IsActive { get; set; }
        public Guid? LinkedProductId { get; set; }
        public string? LinkedProductName { get; set; }
        public int? LinkedProductStockQuantity { get; set; }
        public Guid? LinkedServiceId { get; set; }
        public string? LinkedServiceName { get; set; }
        public string? LinkedCategoryName { get; set; }
        public IEnumerable<InventoryTransactionDto> Transactions { get; set; } = new List<InventoryTransactionDto>();
        public IEnumerable<InvoiceItemUsageDto> UsageRecords { get; set; } = new List<InvoiceItemUsageDto>();
    }

    public record InventoryTransactionDto(
        Guid Id,
        InventoryTransactionType Type,
        decimal Quantity,
        Guid? ReferenceInvoiceItemId,
        string? Note,
        Guid CreatedBy,
        DateTime CreatedAt
    );

    public record InvoiceItemUsageDto(
        Guid Id,
        Guid InvoiceItemId,
        decimal QuantityUsed
    );
}