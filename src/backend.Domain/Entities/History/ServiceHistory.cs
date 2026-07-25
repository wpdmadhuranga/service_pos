namespace backend.Domain.Entities.History
{
    public class ServiceHistory
    {
        public Guid Id { get; set; }

        // Traceability back to the source of truth - not a hard FK by design,
        // so this table reads standalone without joining across schemas.
        public Guid InvoiceId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }

        public DateTime ServiceDate { get; set; }

        // Snapshots - copied at write-time so reports never need to join back.
        public string CustomerNameSnapshot { get; set; } = string.Empty;
        public string CustomerPhoneSnapshot { get; set; } = string.Empty;
        public string VehiclePlateSnapshot { get; set; } = string.Empty;
        public string? VehicleMakeModelSnapshot { get; set; }

        public int? OdometerAtService { get; set; }
        public string? ServicesSummary { get; set; } // e.g. "Body wash, Engine oil, Oil filter"
        public decimal TotalAmount { get; set; }

        public string? MechanicNotes { get; set; }
        public string? VehicleConditionNotes { get; set; } // e.g. "brake pads worn, recommend replacement soon"

        public DateTime? NextRecommendedDate { get; set; }
        public int? NextRecommendedOdometer { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}