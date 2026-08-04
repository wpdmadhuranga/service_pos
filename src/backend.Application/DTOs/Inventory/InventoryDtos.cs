using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Inventory
{
    public sealed record ProductDto(
        Guid Id,
        Guid? ServiceId,
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

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal CostPrice { get; init; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
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

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? CostPrice { get; init; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
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
}