namespace backend.Domain.Entities.History
{
    public class CustomerServiceSummary
    {
        public Guid CustomerId { get; set; } // PK - one row per customer, not a hard FK (see ServiceHistory)

        public int TotalVisits { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime FirstVisitDate { get; set; }
        public DateTime LastVisitDate { get; set; }
        public decimal AverageSpendPerVisit { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}