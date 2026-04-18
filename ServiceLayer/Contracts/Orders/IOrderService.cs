using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Orders;

namespace ServiceLayer.Contracts.Orders;

public interface IOrderService
{
    Task<CheckoutOrderResponse> CheckoutOrderAsync(
        int userId,
        CheckoutOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<BuyNowOrderResponse> BuyNowAsync(
        int userId,
        BuyNowOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryResponse>> GetOrdersAsync(
        int currentUserId,
        bool canAccessAllOrders,
        GetOrdersRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse?> GetOrderByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderCancelResponse> CancelOrderAsync(
        int userId,
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> AssignStaffAsync(
        int currentUserId,
        int orderId,
        AssignOrderStaffRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderItemsResponse?> GetOrderItemsAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderItemDetailResponse?> GetOrderItemByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        int orderItemId,
        CancellationToken cancellationToken = default);

    Task<OrderStatusUpdatedResponse> UpdateOrderStatusAsync(
        int staffUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingStatusUpdatedResponse> UpdateShippingStatusAsync(
        int staffUserId,
        int orderId,
        UpdateShippingStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderStatusHistoriesResponse?> GetOrderStatusHistoriesAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default);
}
