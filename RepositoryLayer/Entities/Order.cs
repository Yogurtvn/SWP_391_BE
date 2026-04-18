using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public OrderType OrderType { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string? ShippingCode { get; set; }

    public decimal ShippingFee { get; set; }                // Phí vận chuyển đã tính (VNĐ), được lưu khi khách đặt hàng

    public int? ShippingDistrictId { get; set; }            // Mã quận/huyện GHN nơi nhận hàng (dùng để tra cứu lại nếu cần)

    public string? ShippingWardCode { get; set; }           // Mã phường/xã GHN nơi nhận hàng

    public ShippingStatus? ShippingStatus { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public int? StaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public User? Staff { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}
