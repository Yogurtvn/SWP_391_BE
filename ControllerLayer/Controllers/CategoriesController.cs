using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Category;
using ServiceLayer.DTOs.Category.Request;
using ServiceLayer.DTOs.Category.Response;
using ServiceLayer.DTOs.Common;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller xử lý tất cả API liên quan đến Category:
/// - Public: lấy danh sách, xem chi tiết
/// - Admin: tạo, cập nhật, xóa category
/// </summary>
[Route("api/categories")]
[ApiController]
public class CategoriesController(ICategoryService categoryService) : ApiControllerBase
{
    // Inject CategoryService để gọi các nghiệp vụ category
    private readonly ICategoryService _categoryService = categoryService;

    /// <summary>
    /// GET /api/categories
    /// Lấy danh sách categories có phân trang, tìm kiếm, sắp xếp.
    /// Không yêu cầu xác thực (Public API).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDetailResponse>>> GetCategories(
        [FromQuery] GetCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để lấy danh sách categories phân trang
            var result = await _categoryService.GetCategoriesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// GET /api/categories/{categoryId}
    /// Lấy chi tiết 1 category theo id.
    /// Không yêu cầu xác thực (Public API).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<CategoryDetailResponse>> GetCategory(
        int categoryId,
        CancellationToken cancellationToken)
    {
        // Gọi service để lấy chi tiết category
        var result = await _categoryService.GetCategoryByIdAsync(categoryId, cancellationToken);

        // Nếu không tìm thấy, trả về 404
        if (result is null)
        {
            return NotFound(new { errorCode = "CATEGORY_NOT_FOUND", message = "Category not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /api/categories
    /// Admin tạo category mới.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDetailResponse>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để tạo category mới
            var result = await _categoryService.CreateCategoryAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// PUT /api/categories/{categoryId}
    /// Admin cập nhật tên category.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{categoryId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdateCategory(
        int categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để cập nhật category
            var result = await _categoryService.UpdateCategoryAsync(categoryId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// DELETE /api/categories/{categoryId}
    /// Admin xóa category.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{categoryId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteCategory(
        int categoryId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để xóa category
            var result = await _categoryService.DeleteCategoryAsync(categoryId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
