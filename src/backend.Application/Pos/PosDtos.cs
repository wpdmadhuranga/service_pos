using System.ComponentModel.DataAnnotations;
using backend.Domain.Enums;

namespace backend.Application.Pos
{
    public sealed record PosServiceCategoryGroupDto(
        Guid CategoryId,
        string CategoryName,
        int SortOrder,
        IReadOnlyList<PosServiceDto> Services);

    public sealed record PosServiceDto(
        Guid Id,
        string Name,
        string? Description,
        decimal DefaultPrice,
        PricingType PricingType,
        decimal? MinPrice,
        decimal? MaxPrice,
        string? Unit,
        int SortOrder);

    public sealed record PosCustomerSearchResultDto(
        Guid Id,
        string Name,
        string Phone,
        string? Email,
        string? Address);

    public sealed record PosVehicleDto(
        Guid Id,
        Guid CustomerId,
        string PlateNumber,
        string? Make,
        string? Model,
        int? Year,
        string? VehicleType,
        int OdometerReading);

    public sealed record PosCustomerInput
    {
        [Required]
        [StringLength(150)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Phone { get; init; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; init; }

        [StringLength(300)]
        public string? Address { get; init; }

        [StringLength(1000)]
        public string? Notes { get; init; }
    }

    public sealed record PosVehicleInput
    {
        [Required]
        [StringLength(20)]
        public string PlateNumber { get; init; } = string.Empty;

        [StringLength(60)]
        public string? Make { get; init; }

        [StringLength(60)]
        public string? Model { get; init; }

        [Range(1886, 3000)]
        public int? Year { get; init; }

        [StringLength(30)]
        public string? VehicleType { get; init; }

        [Range(0, int.MaxValue)]
        public int? OdometerReading { get; init; }
    }

    public sealed record PosInvoiceItemInput : IValidatableObject
    {
        public Guid? ServiceId { get; init; }

        [StringLength(150)]
        public string? Name { get; init; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? Price { get; init; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; init; } = 1;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ServiceId is null)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    yield return new ValidationResult("Name is required when ServiceId is not provided.", new[] { nameof(Name) });
                }

                if (Price is null)
                {
                    yield return new ValidationResult("Price is required when ServiceId is not provided.", new[] { nameof(Price) });
                }
            }
        }
    }

    public sealed record PosCreateInvoiceRequest : IValidatableObject
    {
        [Required]
        public Guid? UserId { get; init; }

        public Guid? CustomerId { get; init; }
        public PosCustomerInput? Customer { get; init; }

        public Guid? VehicleId { get; init; }
        public PosVehicleInput? Vehicle { get; init; }

        [Range(0, int.MaxValue)]
        public int? OdometerAtService { get; init; }

        [StringLength(1000)]
        public string? Notes { get; init; }

        [Required]
        [MinLength(1)]
        public List<PosInvoiceItemInput> Items { get; init; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CustomerId is null && Customer is null)
            {
                yield return new ValidationResult("Either CustomerId or Customer must be supplied.", new[] { nameof(CustomerId), nameof(Customer) });
            }

            if (CustomerId is not null && Customer is not null)
            {
                yield return new ValidationResult("Provide either CustomerId or Customer, not both.", new[] { nameof(CustomerId), nameof(Customer) });
            }

            if (VehicleId is null && Vehicle is null)
            {
                yield return new ValidationResult("Either VehicleId or Vehicle must be supplied.", new[] { nameof(VehicleId), nameof(Vehicle) });
            }

            if (VehicleId is not null && Vehicle is not null)
            {
                yield return new ValidationResult("Provide either VehicleId or Vehicle, not both.", new[] { nameof(VehicleId), nameof(Vehicle) });
            }
        }
    }

    public sealed record PosUpdateDraftInvoiceRequest : IValidatableObject
    {
        public List<PosInvoiceItemInput>? Items { get; init; }

        [Range(0, double.MaxValue)]
        public decimal? Discount { get; init; }

        [StringLength(1000)]
        public string? Notes { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Items is not null && Items.Count == 0)
            {
                yield return new ValidationResult("Items cannot be empty when supplied.", new[] { nameof(Items) });
            }
        }
    }

    public sealed record PosRecordPaymentRequest : IValidatableObject
    {
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; init; }

        [Required]
        public PaymentMethod Method { get; init; }

        public DateTime? PaidAt { get; init; }

        [StringLength(100)]
        public string? ReferenceNo { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PaidAt is not null && PaidAt.Value.Kind == DateTimeKind.Unspecified)
            {
                yield return new ValidationResult("PaidAt must be UTC or local date-time with a known kind.", new[] { nameof(PaidAt) });
            }
        }
    }

    public sealed record PosInvoiceCustomerDto(
        Guid Id,
        string Name,
        string Phone,
        string? Email,
        string? Address);

    public sealed record PosInvoiceVehicleDto(
        Guid Id,
        string PlateNumber,
        string? Make,
        string? Model,
        int? Year,
        string? VehicleType,
        int OdometerReading);

    public sealed record PosInvoiceItemDto(
        Guid Id,
        Guid? ServiceId,
        string NameSnapshot,
        decimal PriceSnapshot,
        int Quantity,
        decimal LineTotal);

    public sealed record PosPaymentDto(
        Guid Id,
        decimal Amount,
        PaymentMethod Method,
        DateTime PaidAt,
        string? ReferenceNo);

    public sealed record PosInvoiceDetailDto(
        Guid Id,
        string InvoiceNumber,
        Guid CustomerId,
        Guid VehicleId,
        Guid UserId,
        int? OdometerAtService,
        string Status,
        decimal Subtotal,
        decimal Discount,
        decimal Tax,
        decimal Total,
        decimal AmountPaid,
        string PaymentStatus,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        PosInvoiceCustomerDto Customer,
        PosInvoiceVehicleDto Vehicle,
        IReadOnlyList<PosInvoiceItemDto> Items,
        IReadOnlyList<PosPaymentDto> Payments);
}