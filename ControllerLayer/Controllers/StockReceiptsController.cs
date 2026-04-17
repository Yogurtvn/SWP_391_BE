using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;
using System.Security.Claims;

namespace ControllerLayer.Controllers;

[Route("api/stock-receipts")]
[ApiController]
public class StockReceiptsController(IStockReceiptService stockReceiptService) : ControllerBase
{
    private readonly IStockReceiptService _stockReceiptService = stockReceiptService;

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<StockReceiptDtoResponse>> CreateStockReceipt(
        [FromBody] CreateStockReceiptRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var staffId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        try
        {
            var result = await _stockReceiptService.CreateStockReceiptAsync(request, staffId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<StockReceiptListDtoResponse>>> GetStockReceipts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? variantId = null,
        [FromQuery] int? staffId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var result = await _stockReceiptService.GetStockReceiptsAsync(
            new PaginationRequest(page, pageSize),
            variantId,
            staffId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{receiptId:int}")]
    public async Task<ActionResult<StockReceiptDtoResponse>> GetStockReceipt(int receiptId, CancellationToken cancellationToken)
    {
        var result = await _stockReceiptService.GetStockReceiptByIdAsync(receiptId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Stock receipt not found." });
        }

        return Ok(result);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }
}
