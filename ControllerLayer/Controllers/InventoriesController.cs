using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;

namespace ControllerLayer.Controllers;

[Route("api/inventories")]  // Route mặc định: /api/inventories
[ApiController]              // Tự động validate model state và trả 400 nếu invalid
public class InventoriesController(IInventoryService inventoryService) : ControllerBase
{
    private readonly IInventoryService _inventoryService = inventoryService; // Inject service qua primary constructor

    // GET /api/inventories?page=1&pageSize=20&variantId=10&productId=5&isPreOrderAllowed=true&search=keyword
    // Lấy danh sách inventory có phân trang, lọc và tìm kiếm
    [AllowAnonymous]  // Ai cũng có thể xem danh sách tồn kho
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryListDtoResponse>>> GetInventories(
        [FromQuery] int page = 1,                          // Trang hiện tại, mặc định = 1
        [FromQuery] int pageSize = 20,                     // Số item mỗi trang, mặc định = 20
        [FromQuery] int? variantId = null,                 // Lọc theo variant cụ thể (optional)
        [FromQuery] int? productId = null,                 // Lọc theo product (optional)
        [FromQuery] bool? isPreOrderAllowed = null,        // Lọc theo trạng thái pre-order (optional)
        [FromQuery] string? search = null,                 // Từ khóa tìm kiếm (optional)
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;                            // Đảm bảo page không âm
        if (pageSize < 1) pageSize = 20;                   // Đảm bảo pageSize hợp lệ

        var result = await _inventoryService.GetInventoriesAsync(
            new PaginationRequest(page, pageSize),         // Tạo PaginationRequest với page và pageSize
            variantId,                                     // Truyền filter variantId
            productId,                                     // Truyền filter productId
            isPreOrderAllowed,                             // Truyền filter isPreOrderAllowed
            search,                                        // Truyền search keyword
            cancellationToken);
        return Ok(result);                                 // Trả về 200 OK với danh sách inventory phân trang
    }

    // GET /api/inventories/{variantId}
    // Lấy chi tiết inventory của 1 variant
    [AllowAnonymous]  // Ai cũng có thể xem chi tiết tồn kho
    [HttpGet("{variantId:int}")]  // Route constraint: variantId phải là số nguyên
    public async Task<ActionResult<InventoryDtoResponse>> GetInventory(int variantId, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryByVariantIdAsync(variantId, cancellationToken); // Gọi service lấy chi tiết

        if (result is null)
        {
            return NotFound(new { message = "Inventory not found." }); // 404 nếu không tìm thấy inventory cho variant này
        }

        return Ok(result);                                 // 200 OK với chi tiết inventory
    }

    // PUT /api/inventories/{variantId}
    // Admin/Staff cập nhật tồn kho trực tiếp
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin,Staff")])
    [HttpPut("{variantId:int}")]  // Route constraint: variantId phải là số nguyên
    public async Task<ActionResult> UpdateInventory(int variantId, [FromBody] UpdateInventoryRequest request, CancellationToken cancellationToken)
    {
        var success = await _inventoryService.UpdateInventoryAsync(variantId, request, cancellationToken); // Gọi service cập nhật

        if (!success)
        {
            return NotFound(new { message = "Inventory not found." }); // 404 nếu không tìm thấy
        }

        return Ok(new { message = "Inventory updated" }); // 200 OK khi cập nhật thành công
    }

    // PATCH /api/inventories/{variantId}/pre-orders
    // Bật/tắt cài đặt pre-order riêng
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin,Staff")])
    [HttpPatch("{variantId:int}/pre-orders")]  // Route: /api/inventories/{variantId}/pre-orders
    public async Task<ActionResult> UpdatePreOrder(int variantId, [FromBody] UpdatePreOrderRequest request, CancellationToken cancellationToken)
    {
        var success = await _inventoryService.UpdatePreOrderAsync(variantId, request, cancellationToken); // Gọi service cập nhật pre-order

        if (!success)
        {
            return NotFound(new { message = "Inventory not found." }); // 404 nếu không tìm thấy
        }

        return Ok(new { message = "Pre-order setting updated" }); // 200 OK khi cập nhật pre-order thành công
    }
}
