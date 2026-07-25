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
        public string? ReferenceNo { get; set; } // bank ref, card auth code, etc.

        // Navigation
        public Invoice Invoice { get; set; } = null!;
    }
}