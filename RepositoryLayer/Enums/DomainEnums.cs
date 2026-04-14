namespace RepositoryLayer.Enums;

public enum UserRole : byte
{
    Admin = 1,
    Staff = 2,
    Customer = 3
}

public enum OrderType : byte
{
    Ready = 1,
    PreOrder = 2,
    Prescription = 3
}

public enum OrderStatus : byte
{
    Pending = 1,
    Confirmed = 2,
    AwaitingStock = 3,
    Processing = 4,
    Shipped = 5,
    Completed = 6,
    Cancelled = 7
}

public enum ShippingStatus : byte
{
    Pending = 1,
    Picking = 2,
    Delivering = 3,
    Delivered = 4,
    Failed = 5
}

public enum PrescriptionStatus : byte
{
    Submitted = 1,
    Reviewing = 2,
    NeedMoreInfo = 3,
    Approved = 4,
    Rejected = 5,
    InProduction = 6
}

public enum PaymentMethod : byte
{
    COD = 1,
    VNPay = 2,
    Momo = 3
}

public enum PaymentStatus : byte
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
