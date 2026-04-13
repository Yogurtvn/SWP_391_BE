namespace RepositoryLayer.Entities;

public class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<Cart> Carts { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<Order> SalesOrders { get; set; } = [];

    public ICollection<Order> OperationsOrders { get; set; } = [];

    public ICollection<PrescriptionSpec> PrescriptionSpecs { get; set; } = [];

    public ICollection<PrescriptionSpec> VerifiedPrescriptionSpecs { get; set; } = [];

    public ICollection<SupplyReceipt> SupplyReceipts { get; set; } = [];

    public ICollection<OrderStatusHistory> UpdatedOrderStatusHistories { get; set; } = [];
}
