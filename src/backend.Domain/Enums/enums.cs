namespace backend.Domain.Enums
{
    public enum PricingType
    {
        Fixed,
        Variable
    }

    public enum InvoiceStatus
    {
        Draft,
        Completed,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        BankTransfer = 2
    }

    public enum UserRole
    {
        Admin,
        Staff
    }

    public enum PaymentStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid
    }
}