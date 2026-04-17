using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Category;
using ServiceLayer.DTOs.Category.Request;
using ServiceLayer.DTOs.Category.Response;
using ServiceLayer.DTOs.Common;
using ServiceLayer.Exceptions;
using System.Linq.Expressions;

namespace ServiceLayer.Services.CategoryManagement;

/// <summary>
/// Service xử lý toàn bộ nghiệp vụ liên quan đến Category.
/// </summary>
public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    // Inject UnitOfWork để truy cập repository
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Lấy danh sách categories có phân trang, tìm kiếm, sắp xếp (Public API).
    /// </summary>
    public async Task<PagedResult<CategoryDetailResponse>> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken)
    {
        // Xây dựng bộ lọc tìm kiếm theo tên category
        Expression<Func<Category, bool>>? filter = null;
        var search = request.Search?.Trim().ToLower();

        if (!string.IsNullOrEmpty(search))
        {
            // Lọc categories có tên chứa từ khóa tìm kiếm (không phân biệt hoa thường)
            filter = c => c.CategoryName.ToLower().Contains(search);
        }

        // Xây dựng hàm sắp xếp
        var orderBy = BuildCategoryOrderBy(request.SortBy, request.SortOrder);

        // Tạo PaginationRequest từ page và pageSize
        var paginationRequest = new PaginationRequest(request.Page, request.PageSize);

        // Gọi repository để lấy dữ liệu phân trang
        var pagedResult = await _unitOfWork.Repository<Category>().GetPagedAsync(
            paginationRequest,
            filter: filter,
            orderBy: orderBy,
            tracked: false,
            cancellationToken: cancellationToken);

        // Map từng entity sang response DTO
        var items = pagedResult.Items.Select(c => new CategoryDetailResponse
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName
        }).ToList();

        // Trả về kết quả phân trang với dữ liệu đã map
        return PagedResult<CategoryDetailResponse>.Create(items, pagedResult.Page, pagedResult.PageSize, pagedResult.TotalItems);
    }

    /// <summary>
    /// Lấy chi tiết 1 category theo id (Public API).
    /// </summary>
    public async Task<CategoryDetailResponse?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        // Tìm category trong database theo id
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId);

        // Nếu không tìm thấy, trả về null (controller sẽ xử lý trả 404)
        if (category is null)
        {
            return null;
        }

        // Map entity sang response DTO
        return new CategoryDetailResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName
        };
    }

    /// <summary>
    /// Admin tạo category mới (kiểm tra tên trùng).
    /// </summary>
    public async Task<CategoryDetailResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        // Kiểm tra tên category đã tồn tại chưa (không phân biệt hoa thường)
        var nameExists = await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.CategoryName.ToLower() == request.CategoryName.Trim().ToLower());

        if (nameExists)
        {
            throw new ApiException(409, "CATEGORY_ALREADY_EXISTS", "Category name already exists");
        }

        // Tạo entity Category mới
        var category = new Category
        {
            CategoryName = request.CategoryName.Trim()
        };

        // Lưu vào database
        await _unitOfWork.Repository<Category>().AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Trả về thông tin category vừa tạo
        return new CategoryDetailResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName
        };
    }

    /// <summary>
    /// Admin cập nhật tên category.
    /// </summary>
    public async Task<MessageResponse> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        // Tìm category cần cập nhật
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId);

        // Nếu không tìm thấy, ném lỗi 404
        if (category is null)
        {
            throw new ApiException(404, "CATEGORY_NOT_FOUND", "Category not found");
        }

        // Kiểm tra tên mới có bị trùng với category khác không
        var nameExists = await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.CategoryId != categoryId &&
                             c.CategoryName.ToLower() == request.CategoryName.Trim().ToLower());

        if (nameExists)
        {
            throw new ApiException(409, "CATEGORY_ALREADY_EXISTS", "Category name already exists");
        }

        // Cập nhật tên category
        category.CategoryName = request.CategoryName.Trim();

        // Lưu thay đổi vào database
        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Category updated" };
    }

    /// <summary>
    /// Admin xóa category (hard delete).
    /// </summary>
    public async Task<MessageResponse> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        // Tìm category cần xóa
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId);

        // Nếu không tìm thấy, ném lỗi 404
        if (category is null)
        {
            throw new ApiException(404, "CATEGORY_NOT_FOUND", "Category not found");
        }

        // Xóa category khỏi database (hard delete)
        _unitOfWork.Repository<Category>().Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Category deleted" };
    }

    /// <summary>
    /// Xây dựng hàm sắp xếp category dựa trên sortBy và sortOrder.
    /// </summary>
    private static Func<IQueryable<Category>, IOrderedQueryable<Category>>? BuildCategoryOrderBy(string? sortBy, string? sortOrder)
    {
        // Xác định chiều sắp xếp: mặc định là tăng dần (asc)
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        // Áp dụng sắp xếp theo trường được chỉ định
        return sortBy?.ToLower() switch
        {
            "categoryname" => isDescending
                ? q => q.OrderByDescending(c => c.CategoryName)
                : q => q.OrderBy(c => c.CategoryName),
            _ => isDescending // Mặc định sắp xếp theo CategoryId
                ? q => q.OrderByDescending(c => c.CategoryId)
                : q => q.OrderBy(c => c.CategoryId)
        };
    }
}
