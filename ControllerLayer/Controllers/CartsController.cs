using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Cart;
using ServiceLayer.DTOs.Cart.Request;
using ServiceLayer.DTOs.Cart.Response;
using ServiceLayer.DTOs.Common;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/carts")]
[ApiController]
public class CartsController(ICartService cartService) : ApiControllerBase
{
    private readonly ICartService _cartService = cartService;

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    public async Task<ActionResult<CartDetailResponse>> GetMyCart(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.GetMyCartAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("me/items")]
    public async Task<ActionResult<MessageResponse>> ClearMyCart(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.ClearMyCartAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("me/items")]
    public async Task<ActionResult<StandardCartItemCreatedResponse>> AddStandardItem(
        [FromBody] AddStandardCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.AddStandardItemAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("me/items/{cartItemId:int}")]
    public async Task<ActionResult<StandardCartItemUpdatedResponse>> UpdateStandardItem(
        int cartItemId,
        [FromBody] UpdateStandardCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.UpdateStandardItemAsync(userId, cartItemId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("me/items/{cartItemId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteStandardItem(
        int cartItemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.DeleteStandardItemAsync(userId, cartItemId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("me/prescription-items")]
    public async Task<ActionResult<PrescriptionCartItemCreatedResponse>> AddPrescriptionItem(
        [FromBody] UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.AddPrescriptionItemAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("me/prescription-items/{cartItemId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdatePrescriptionItem(
        int cartItemId,
        [FromBody] UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.UpdatePrescriptionItemAsync(userId, cartItemId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("me/prescription-items/{cartItemId:int}")]
    public async Task<ActionResult<MessageResponse>> DeletePrescriptionItem(
        int cartItemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _cartService.DeletePrescriptionItemAsync(userId, cartItemId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
