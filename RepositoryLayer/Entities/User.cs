using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public Cart? Cart { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<Order> HandledOrders { get; set; } = [];

    public ICollection<PrescriptionSpec> PrescriptionSpecs { get; set; } = [];

    public ICollection<PrescriptionSpec> VerifiedPrescriptionSpecs { get; set; } = [];

    public ICollection<StockReceipt> StockReceipts { get; set; } = [];

    public ICollection<OrderStatusHistory> UpdatedOrderStatusHistories { get; set; } = [];
}
