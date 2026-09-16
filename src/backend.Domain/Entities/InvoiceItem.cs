namespace backend.Domain.Entities
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? ServiceId { get; set; } 
        public Guid? ProductId { get; set; }
        public string? BrandSnapshot { get; set; }
        public string NameSnapshot { get; set; } = string.Empty;
        public decimal PriceSnapshot { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal LineTotal { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public Service? Service { get; set; }
        public Product? Product { get; set; }
    }
}