namespace backend.Domain.Entities
{
    public class Vehicle
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? VehicleType { get; set; } // car / van / motorbike / three-wheeler
        public int OdometerReading { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public Customer Customer { get; set; } = null!;
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}