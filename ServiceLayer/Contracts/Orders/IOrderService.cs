using ServiceLayer.DTOs.Orders;

namespace ServiceLayer.Contracts.Orders;

public interface IOrderService
{
    Task<OrderDetailResponse> CheckoutReadyOrderAsync(
        int userId,
        ReadyOrderCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummaryResponse>> GetMyOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse?> GetMyOrderByIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse?> UpdateOrderStatusAsync(
        int staffUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<CancelOrderResult> CancelMyOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
}
