using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.DTOs.Orders;
using System.Security.Claims;

namespace ControllerLayer.Controllers;

[Authorize]
[Route("api/orders")]
[ApiController]
public class OrdersReadyController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [HttpPost("ready/checkout")]
    public async Task<ActionResult<OrderDetailResponse>> CheckoutReadyOrder(
        [FromBody] ReadyOrderCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        try
        {
            var result = await _orderService.CheckoutReadyOrderAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetOrderById), new { id = result.OrderId }, result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> GetMyOrders(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        var result = await _orderService.GetMyOrdersAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrderById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        var result = await _orderService.GetMyOrderByIdAsync(userId, id, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Order not found." });
        }

        return Ok(result);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<OrderDetailResponse>> CancelOrder(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        try
        {
            var result = await _orderService.CancelMyOrderAsync(userId, id, cancellationToken);

            if (result.Succeeded)
            {
                return Ok(result.Order);
            }

            return result.ErrorCode switch
            {
                "ORDER_NOT_FOUND" => NotFound(new { message = result.Message }),
                "ORDER_ALREADY_CANCELLED" => Conflict(new { message = result.Message }),
                "ORDER_CANNOT_BE_CANCELLED" => Conflict(new { message = result.Message }),
                "PAYMENT_ALREADY_COMPLETED" => Conflict(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("ready/{id:int}/status")]
    public async Task<ActionResult<OrderDetailResponse>> UpdateReadyOrderStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(userId, id, request, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Order not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }
}
