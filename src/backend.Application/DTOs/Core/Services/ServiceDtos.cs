using System.ComponentModel.DataAnnotations;
using backend.Domain.Enums;
using backend.Application.DTOs.Core.ServiceCategory;
using backend.Application.DTOs.Inventory.Products;

namespace backend.Application.DTOs.Core.Services
{
    public sealed record ServiceDto(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        string Name,
        string? Description,
        decimal DefaultPrice,
        PricingType PricingType,
        decimal? MinPrice,
        decimal? MaxPrice,
        string? Unit,
        bool IsActive,
        int SortOrder);

    public sealed record ServiceCreateRequest : IValidatableObject
    {
        [Required]
        public Guid? CategoryId { get; init; }

        [Required]
        [StringLength(150)]
        public string Name { get; init; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal DefaultPrice { get; init; }

        [Required]
        public PricingType PricingType { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? MinPrice { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? MaxPrice { get; init; }

        [StringLength(20)]
        public string? Unit { get; init; }

        public bool IsActive { get; init; } = true;

        [Range(0, int.MaxValue)]
        public int SortOrder { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PricingType == PricingType.Variable && MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value)
            {
                yield return new ValidationResult("MinPrice cannot be greater than MaxPrice.", new[] { nameof(MinPrice), nameof(MaxPrice) });
            }
        }
    }

    public sealed record ServiceUpdateRequest : IValidatableObject
    {
        public Guid? CategoryId { get; init; }

        [StringLength(150)]
        public string? Name { get; init; }

        [StringLength(500)]
        public string? Description { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? DefaultPrice { get; init; }

        public PricingType? PricingType { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? MinPrice { get; init; }

        // [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? MaxPrice { get; init; }

        [StringLength(20)]
        public string? Unit { get; init; }

        public bool? IsActive { get; init; }

        [Range(0, int.MaxValue)]
        public int? SortOrder { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value)
            {
                yield return new ValidationResult("MinPrice cannot be greater than MaxPrice.", new[] { nameof(MinPrice), nameof(MaxPrice) });
            }
        }
    }

     public class ServiceGetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DefaultPrice { get; set; }
        public string PricingType { get; set; } = string.Empty; 
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }

        public ServiceCategoryDto Category { get; set; } = null!;
        public List<ProductDto> Products { get; set; } = new();
    }

}