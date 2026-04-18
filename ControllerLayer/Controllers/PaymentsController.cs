using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController(IPaymentService paymentService) : ApiControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpPost]
    public async Task<ActionResult<CreatePaymentResponse>> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _paymentService.CreatePaymentAsync(
                userId,
                CanAccessAllPayments(),
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpGet]
    public async Task<ActionResult> GetPayments(
        [FromQuery] GetPaymentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.GetPaymentsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet("{paymentId:int}")]
    public async Task<ActionResult<PaymentDetailResponse>> GetPaymentById(
        int paymentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _paymentService.GetPaymentByIdAsync(
                userId,
                CanAccessAllPayments(),
                paymentId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "PAYMENT_NOT_FOUND", message = "Payment not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPatch("{paymentId:int}/statuses")]
    public async Task<ActionResult<PaymentStatusUpdatedResponse>> UpdatePaymentStatus(
        int paymentId,
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpGet("{paymentId:int}/histories")]
    public async Task<ActionResult<PaymentHistoriesResponse>> GetPaymentHistories(
        int paymentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.GetPaymentHistoriesAsync(paymentId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnpayReturn(CancellationToken cancellationToken)
    {
        await _paymentService.HandleVnpayReturnAsync(ToQueryDictionary(Request.Query), cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnpayIpn(CancellationToken cancellationToken)
    {
        var result = await _paymentService.HandleVnpayIpnAsync(ToQueryDictionary(Request.Query), cancellationToken);
        return Content($"RspCode={result.RspCode}&Message={Uri.EscapeDataString(result.Message)}", "text/plain");
    }

    private bool CanAccessAllPayments()
    {
        return User.IsInRole("Admin") || User.IsInRole("Staff");
    }

    private static IReadOnlyDictionary<string, string> ToQueryDictionary(IQueryCollection query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in query)
        {
            values[item.Key] = item.Value.ToString();
        }

        return values;
    }
}
