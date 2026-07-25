namespace backend.Domain.Entities
{
    public class ServiceCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Wash, Fluids, Filters, Lubrication, Other
        public int SortOrder { get; set; }

        // Navigation
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}