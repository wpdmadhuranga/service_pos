using backend.Domain.Enums;

namespace backend.Domain.Entities

{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public DateTime PaidAt { get; set; }
        public string? ReferenceNo { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }
}