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
        Cash,
        Card,
        BankTransfer
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