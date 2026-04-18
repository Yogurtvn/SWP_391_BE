using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;

namespace ControllerLayer.Controllers;

[Route("api/inventories")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class InventoriesController(IInventoryService inventoryService) : ControllerBase
{
    private readonly IInventoryService _inventoryService = inventoryService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryListDtoResponse>>> GetInventories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? variantId = null,
        [FromQuery] int? productId = null,
        [FromQuery] bool? isPreOrderAllowed = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
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

        var result = await _inventoryService.GetInventoriesAsync(
            new PaginationRequest(page, pageSize),
            variantId,
            productId,
            isPreOrderAllowed,
            search,
            sortBy,
            sortOrder,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{variantId:int}")]
    public async Task<ActionResult<InventoryDtoResponse>> GetInventory(int variantId, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryByVariantIdAsync(variantId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Inventory not found." });
        }

        return Ok(result);
    }

    [HttpPut("{variantId:int}")]
    public async Task<ActionResult> UpdateInventory(
        int variantId,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _inventoryService.UpdateInventoryAsync(variantId, request, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Inventory not found." });
        }

        return Ok(new { message = "Inventory updated" });
    }

    [HttpPatch("{variantId:int}/pre-orders")]
    public async Task<ActionResult> UpdatePreOrder(
        int variantId,
        [FromBody] UpdatePreOrderRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _inventoryService.UpdatePreOrderAsync(variantId, request, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Inventory not found." });
        }

        return Ok(new { message = "Pre-order setting updated" });
    }
}
