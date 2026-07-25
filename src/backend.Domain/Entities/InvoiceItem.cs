namespace backend.Domain.Entities
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? ServiceId { get; set; } // null for one-off custom items

        // Snapshot fields - copied at time of invoice, never change even if
        // the underlying Service's price/name changes later.
        public string NameSnapshot { get; set; } = string.Empty;
        public decimal PriceSnapshot { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal LineTotal { get; set; }

        // Navigation
        public Invoice Invoice { get; set; } = null!;
        public Service? Service { get; set; }
    }
}