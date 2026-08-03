using System.ComponentModel.DataAnnotations;
using backend.Application.Common.Models;
using backend.Domain.Enums;

namespace backend.Application.History
{
    public sealed record HistoryInvoiceListQueryRequest : IValidatableObject
    {
        public string? Status { get; init; }
        public string? PaymentStatus { get; init; }
        public Guid? CustomerId { get; init; }
        public DateTime? DateFrom { get; init; }
        public DateTime? DateTo { get; init; }
        public string? Search { get; init; }

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [StringLength(50)]
        public string SortBy { get; init; } = "createdAt";

        [StringLength(4)]
        public string SortDir { get; init; } = "desc";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var allowedStatuses = new[] { "draft", "completed", "cancelled" };
            if (!string.IsNullOrWhiteSpace(Status) && !allowedStatuses.Contains(Status.Trim().ToLowerInvariant()))
            {
                yield return new ValidationResult("Status must be Draft, Completed, or Cancelled.", new[] { nameof(Status) });
            }

            var allowedPaymentStatuses = new[] { "unpaid", "partial", "paid" };
            if (!string.IsNullOrWhiteSpace(PaymentStatus) && !allowedPaymentStatuses.Contains(PaymentStatus.Trim().ToLowerInvariant()))
            {
                yield return new ValidationResult("PaymentStatus must be Unpaid, Partial, or Paid.", new[] { nameof(PaymentStatus) });
            }

            var allowedSortBy = new[] { "createdat", "invoicenumber", "customername", "total", "status" };
            if (!allowedSortBy.Contains(SortBy.Trim().ToLowerInvariant()))
            {
                yield return new ValidationResult("SortBy must be createdAt, invoiceNumber, customerName, total, or status.", new[] { nameof(SortBy) });
            }

            var allowedSortDir = new[] { "asc", "desc" };
            if (!allowedSortDir.Contains(SortDir.Trim().ToLowerInvariant()))
            {
                yield return new ValidationResult("SortDir must be asc or desc.", new[] { nameof(SortDir) });
            }

            if (DateFrom is not null && DateTo is not null && DateFrom > DateTo)
            {
                yield return new ValidationResult("DateFrom must be earlier than or equal to DateTo.", new[] { nameof(DateFrom), nameof(DateTo) });
            }
        }
    }

    public sealed record HistoryCustomerDto(
        Guid Id,
        string Name,
        string Phone,
        string? Email,
        string? Address);

    public sealed record HistoryVehicleDto(
        Guid Id,
        string PlateNumber,
        string? Make,
        string? Model,
        int? Year,
        string? VehicleType,
        int OdometerReading);

    public sealed record HistoryInvoiceItemDto(
        Guid Id,
        Guid? ServiceId,
        string NameSnapshot,
        decimal PriceSnapshot,
        int Quantity,
        decimal LineTotal);

    public sealed record HistoryPaymentDto(
        Guid Id,
        decimal Amount,
        PaymentMethod Method,
        DateTime PaidAt,
        string? ReferenceNo);

    public sealed record HistoryInvoiceListItemDto(
        Guid Id,
        string InvoiceNumber,
        string Status,
        string PaymentStatus,
        Guid CustomerId,
        string CustomerName,
        string CustomerPhone,
        Guid VehicleId,
        string VehiclePlateNumber,
        decimal Subtotal,
        decimal Discount,
        decimal Tax,
        decimal Total,
        decimal AmountPaid,
        decimal BalanceDue,
        int? OdometerAtService,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record HistoryInvoiceDetailDto(
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
        HistoryCustomerDto Customer,
        HistoryVehicleDto Vehicle,
        IReadOnlyList<HistoryInvoiceItemDto> Items,
        IReadOnlyList<HistoryPaymentDto> Payments);
}