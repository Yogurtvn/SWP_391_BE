namespace ServiceLayer.DTOs.Shipping.Response;

// DTO trả về cho client sau khi tính phí ship
public class ShippingFeeResponse
{
    public decimal TotalFee { get; set; }           // Tổng phí vận chuyển (VNĐ)

    public decimal ServiceFee { get; set; }         // Phí dịch vụ vận chuyển chính

    public decimal InsuranceFee { get; set; }       // Phí bảo hiểm hàng hóa

    public string ExpectedDeliveryTime { get; set; } = string.Empty; // Thời gian giao hàng dự kiến (VD: "2-3 ngày")
}
