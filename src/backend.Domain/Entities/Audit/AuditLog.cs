namespace backend.Domain.Entities.Audit
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string TableName { get; set; } = string.Empty; // e.g. "Invoices", "Services"
        public Guid RecordId { get; set; } // the row that changed
        public string Action { get; set; } = string.Empty; // Create / Update / Delete

        public Guid? ChangedBy { get; set; } // null if system-triggered
        public DateTime ChangedAt { get; set; }

        public string? OldValues { get; set; } // serialized JSON snapshot before change
        public string? NewValues { get; set; } // serialized JSON snapshot after change
        public string? IpAddress { get; set; }
    }
}