using backend.Domain.Enums;

namespace backend.Domain.Entities

{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneOrEmail { get; set; } = string.Empty; // login identifier
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Staff;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}