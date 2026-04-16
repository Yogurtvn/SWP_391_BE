using RepositoryLayer.Common;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;
using InventoryEntity = RepositoryLayer.Entities.Inventory; // Alias để tránh xung đột namespace với folder Inventory

namespace ServiceLayer.Services.InventoryManagement; // Dùng "InventoryManagement" thay vì "Inventory" để tránh xung đột namespace với RepositoryLayer.Entities.Inventory

// Service xử lý logic nghiệp vụ quản lý Inventory (tồn kho), sử dụng UnitOfWork + GenericRepository
public class InventoryService(IUnitOfWork unitOfWork) : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork; // Inject UnitOfWork để truy cập repository và quản lý transaction

    // Lấy danh sách inventory có phân trang, lọc và tìm kiếm (copy logic pagination từ PolicyService)
    public async Task<PagedResult<InventoryListDtoResponse>> GetInventoriesAsync(
        PaginationRequest paginationRequest,
        int? variantId,                                   // Lọc theo variant cụ thể
        int? productId,                                   // Lọc theo product
        bool? isPreOrderAllowed,                          // Lọc theo trạng thái pre-order
        string? search,                                   // Tìm kiếm
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest); // Kiểm tra paginationRequest không được null

        var repository = _unitOfWork.Repository<InventoryEntity>(); // Lấy repository cho Inventory entity

        var pagedInventories = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,         // Truyền thông tin phân trang (page, pageSize)
            filter: inventory =>
                (!variantId.HasValue || inventory.VariantId == variantId.Value) &&           // Lọc theo variantId nếu có
                (!productId.HasValue || inventory.Variant.ProductId == productId.Value) &&   // Lọc theo productId qua navigation property
                (!isPreOrderAllowed.HasValue || inventory.IsPreOrderAllowed == isPreOrderAllowed.Value), // Lọc theo trạng thái pre-order nếu có
            orderBy: query => query.OrderBy(inventory => inventory.VariantId),               // Sắp xếp theo VariantId tăng dần
            includeProperties: "Variant",                 // Include navigation property Variant để lọc theo ProductId
            tracked: false,                               // Không cần track vì chỉ đọc dữ liệu
            cancellationToken: cancellationToken);

        var items = pagedInventories.Items
            .Select(MapToListDto)                         // Chuyển đổi từ Entity sang DTO danh sách
            .ToList();

        return PagedResult<InventoryListDtoResponse>.Create( // Tạo PagedResult mới với DTO items (copy pattern từ PolicyService)
            items,
            pagedInventories.Page,
            pagedInventories.PageSize,
            pagedInventories.TotalItems);
    }

    // Lấy chi tiết inventory theo VariantId
    public async Task<InventoryDtoResponse?> GetInventoryByVariantIdAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>(); // Lấy repository cho Inventory entity

        var inventory = await repository.GetByIdAsync(variantId);   // Tìm inventory theo primary key (VariantId)

        return inventory is null ? null : MapToDto(inventory);      // Trả về null nếu không tìm thấy, hoặc DTO nếu có
    }

    // Admin/Staff cập nhật tồn kho trực tiếp (PUT)
    public async Task<bool> UpdateInventoryAsync(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>(); // Lấy repository cho Inventory entity

        var inventory = await repository.GetByIdAsync(variantId);   // Tìm inventory cần update (tracked mặc định)

        if (inventory is null)
        {
            return false;                                           // Không tìm thấy → trả về false
        }

        // Cập nhật tất cả các field từ request
        inventory.Quantity = request.Quantity;                       // Cập nhật số lượng tồn kho
        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;    // Cập nhật trạng thái pre-order
        inventory.ExpectedRestockDate = request.ExpectedRestockDate; // Cập nhật ngày dự kiến nhập hàng
        inventory.PreOrderNote = request.PreOrderNote?.Trim();      // Cập nhật ghi chú pre-order (trim khoảng trắng)

        repository.Update(inventory);                               // Đánh dấu entity đã thay đổi
        await _unitOfWork.SaveChangesAsync(cancellationToken);      // Lưu thay đổi vào DB

        return true;                                                // Cập nhật thành công
    }

    // Bật/tắt cài đặt pre-order riêng (PATCH) - chỉ cập nhật các field liên quan pre-order
    public async Task<bool> UpdatePreOrderAsync(int variantId, UpdatePreOrderRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>(); // Lấy repository cho Inventory entity

        var inventory = await repository.GetByIdAsync(variantId);   // Tìm inventory cần update (tracked mặc định)

        if (inventory is null)
        {
            return false;                                           // Không tìm thấy → trả về false
        }

        // Chỉ cập nhật các field pre-order, KHÔNG đụng đến Quantity
        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;    // Bật/tắt pre-order
        inventory.ExpectedRestockDate = request.ExpectedRestockDate; // Cập nhật ngày dự kiến nhập hàng
        inventory.PreOrderNote = request.PreOrderNote?.Trim();      // Cập nhật ghi chú pre-order (trim khoảng trắng)

        repository.Update(inventory);                               // Đánh dấu entity đã thay đổi
        await _unitOfWork.SaveChangesAsync(cancellationToken);      // Lưu thay đổi vào DB

        return true;                                                // Cập nhật thành công
    }

    // Helper method: chuyển đổi từ Entity sang DTO chi tiết (dùng cho GET by ID)
    private static InventoryDtoResponse MapToDto(InventoryEntity inventory)
    {
        return new InventoryDtoResponse
        {
            VariantId = inventory.VariantId,               // Map VariantId
            Quantity = inventory.Quantity,                  // Map Quantity
            IsPreOrderAllowed = inventory.IsPreOrderAllowed // Map IsPreOrderAllowed
        };
    }

    // Helper method: chuyển đổi từ Entity sang DTO danh sách (dùng cho GET list, thêm ExpectedRestockDate)
    private static InventoryListDtoResponse MapToListDto(InventoryEntity inventory)
    {
        return new InventoryListDtoResponse
        {
            VariantId = inventory.VariantId,                     // Map VariantId
            Quantity = inventory.Quantity,                        // Map Quantity
            IsPreOrderAllowed = inventory.IsPreOrderAllowed,     // Map IsPreOrderAllowed
            ExpectedRestockDate = inventory.ExpectedRestockDate  // Map ExpectedRestockDate (nullable)
        };
    }
}
