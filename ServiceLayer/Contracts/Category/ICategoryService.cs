using RepositoryLayer.Common;
using ServiceLayer.DTOs.Category.Request;
using ServiceLayer.DTOs.Category.Response;
using ServiceLayer.DTOs.Common;

namespace ServiceLayer.Contracts.Category;

/// <summary>
/// Interface định nghĩa các nghiệp vụ liên quan đến Category.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Lấy danh sách categories có phân trang, tìm kiếm, sắp xếp (Public).
    /// </summary>
    Task<PagedResult<CategoryDetailResponse>> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết 1 category theo id (Public).
    /// </summary>
    Task<CategoryDetailResponse?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin tạo category mới.
    /// </summary>
    Task<CategoryDetailResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin cập nhật category.
    /// </summary>
    Task<MessageResponse> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin xóa category.
    /// </summary>
    Task<MessageResponse> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
