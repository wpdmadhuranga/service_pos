using backend.Domain.Enums;

namespace backend.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty; // e.g. INV-0142

        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; } // staff member who created it

        public int? OdometerAtService { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Customer Customer { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}