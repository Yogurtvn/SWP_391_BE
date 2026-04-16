using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ControllerLayer.Controllers;

[Route("api/stock-receipts")]  // Route mặc định: /api/stock-receipts
[ApiController]                 // Tự động validate model state và trả 400 nếu invalid
public class StockReceiptsController(IStockReceiptService stockReceiptService) : ControllerBase
{
    private readonly IStockReceiptService _stockReceiptService = stockReceiptService; // Inject service qua primary constructor

    // POST /api/stock-receipts
    // Ghi nhận nhập thêm hàng
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin,Staff")])
    [HttpPost]
    public async Task<ActionResult<StockReceiptDtoResponse>> CreateStockReceipt(
        [FromBody] CreateStockReceiptRequest request,      // Nhận request body chứa variantId, quantityReceived, note
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockReceiptService.CreateStockReceiptAsync(request, cancellationToken); // Gọi service tạo phiếu nhập
            return Ok(result);                             // 200 OK trả về thông tin phiếu nhập vừa tạo
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message }); // 404 nếu VariantId không tồn tại
        }
    }

    // GET /api/stock-receipts?page=1&pageSize=20&variantId=10&staffId=5&fromDate=2026-01-01&toDate=2026-12-31
    // Xem lịch sử nhập hàng có phân trang và lọc
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin,Staff")])
    [HttpGet]
    public async Task<ActionResult<PagedResult<StockReceiptListDtoResponse>>> GetStockReceipts(
        [FromQuery] int page = 1,                          // Trang hiện tại, mặc định = 1
        [FromQuery] int pageSize = 20,                     // Số item mỗi trang, mặc định = 20
        [FromQuery] int? variantId = null,                 // Lọc theo variant cụ thể (optional)
        [FromQuery] int? staffId = null,                   // Lọc theo nhân viên nhập (optional)
        [FromQuery] DateTime? fromDate = null,             // Lọc từ ngày (optional)
        [FromQuery] DateTime? toDate = null,               // Lọc đến ngày (optional)
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;                            // Đảm bảo page không âm
        if (pageSize < 1) pageSize = 20;                   // Đảm bảo pageSize hợp lệ

        var result = await _stockReceiptService.GetStockReceiptsAsync(
            new PaginationRequest(page, pageSize),         // Tạo PaginationRequest với page và pageSize
            variantId,                                     // Truyền filter variantId
            staffId,                                       // Truyền filter staffId
            fromDate,                                      // Truyền filter fromDate
            toDate,                                        // Truyền filter toDate
            cancellationToken);
        return Ok(result);                                 // Trả về 200 OK với danh sách stock receipt phân trang
    }

    // GET /api/stock-receipts/{receiptId}
    // Lấy chi tiết 1 stock receipt
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin,Staff")])
    [HttpGet("{receiptId:int}")]  // Route constraint: receiptId phải là số nguyên
    public async Task<ActionResult<StockReceiptDtoResponse>> GetStockReceipt(int receiptId, CancellationToken cancellationToken)
    {
        var result = await _stockReceiptService.GetStockReceiptByIdAsync(receiptId, cancellationToken); // Gọi service lấy chi tiết

        if (result is null)
        {
            return NotFound(new { message = "Stock receipt not found." }); // 404 nếu không tìm thấy
        }

        return Ok(result);                                 // 200 OK với chi tiết stock receipt
    }
}
