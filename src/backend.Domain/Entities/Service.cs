using backend.Domain.Enums;

namespace backend.Domain.Entities
{
    public class Service
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. "Engine oil (4L)"
        public string? Description { get; set; }
        public decimal DefaultPrice { get; set; }
        public PricingType PricingType { get; set; } = PricingType.Fixed;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Unit { get; set; } // "service" / "litre" / "item"
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ServiceCategory Category { get; set; } = null!;
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}