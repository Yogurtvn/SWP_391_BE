using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Orders;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller xử lý các yêu cầu HTTP liên quan đến Đơn hàng (Checkout, BuyNow, Tra cứu đơn hàng).
/// </summary>
[Route("api/orders")]
[ApiController]
public class OrdersController(IOrderService orderService) : ApiControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [Authorize(Roles = "Customer")]
    [HttpPost("checkout")]
    /// <summary>
    /// API Đặt hàng từ giỏ hàng.
    /// </summary>
    public async Task<ActionResult<CheckoutOrderResponse>> Checkout(
        [FromBody] CheckoutOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.CheckoutOrderAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("buy-now")]
    /// <summary>
    /// API Mua ngay một sản phẩm cụ thể.
    /// </summary>
    public async Task<ActionResult<BuyNowOrderResponse>> BuyNow(
        [FromBody] BuyNowOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.BuyNowAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet]
    /// <summary>
    /// API Lấy danh sách đơn hàng (có phân quyền và bộ lọc).
    /// </summary>
    public async Task<ActionResult> GetOrders(
        [FromQuery] GetOrdersRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.GetOrdersAsync(
                userId,
                CanAccessAllOrders(),
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrderById(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.GetOrderByIdAsync(
                userId,
                CanAccessAllOrders(),
                orderId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "ORDER_NOT_FOUND", message = "Order not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPatch("{orderId:int}/cancel")]
    /// <summary>
    /// API Hủy đơn hàng.
    /// </summary>
    public async Task<ActionResult<OrderCancelResponse>> CancelOrder(
        int orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.CancelOrderAsync(userId, orderId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPatch("{orderId:int}/assign-staff")]
    public async Task<ActionResult<MessageResponse>> AssignStaff(
        int orderId,
        [FromBody] AssignOrderStaffRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.AssignStaffAsync(userId, orderId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet("{orderId:int}/items")]
    public async Task<ActionResult<OrderItemsResponse>> GetOrderItems(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.GetOrderItemsAsync(
                userId,
                CanAccessAllOrders(),
                orderId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "ORDER_NOT_FOUND", message = "Order not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet("{orderId:int}/items/{orderItemId:int}")]
    public async Task<ActionResult<OrderItemDetailResponse>> GetOrderItemById(
        int orderId,
        int orderItemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.GetOrderItemByIdAsync(
                userId,
                CanAccessAllOrders(),
                orderId,
                orderItemId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "ORDER_NOT_FOUND", message = "Order not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPatch("{orderId:int}/statuses")]
    public async Task<ActionResult<OrderStatusUpdatedResponse>> UpdateOrderStatus(
        int orderId,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(userId, orderId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPatch("{orderId:int}/shipping-statuses")]
    public async Task<ActionResult<ShippingStatusUpdatedResponse>> UpdateShippingStatus(
        int orderId,
        [FromBody] UpdateShippingStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.UpdateShippingStatusAsync(userId, orderId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet("{orderId:int}/status-histories")]
    public async Task<ActionResult<OrderStatusHistoriesResponse>> GetOrderStatusHistories(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _orderService.GetOrderStatusHistoriesAsync(
                userId,
                CanAccessAllOrders(),
                orderId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "ORDER_NOT_FOUND", message = "Order not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    private bool CanAccessAllOrders()
    {
        return User.IsInRole("Admin") || User.IsInRole("Staff");
    }
}
