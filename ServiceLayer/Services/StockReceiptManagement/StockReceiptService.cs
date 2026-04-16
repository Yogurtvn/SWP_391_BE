using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Services.StockReceiptManagement; // Dùng "StockReceiptManagement" để tránh xung đột namespace với RepositoryLayer.Entities.StockReceipt

// Service xử lý logic nghiệp vụ quản lý Stock Receipt (phiếu nhập hàng), sử dụng UnitOfWork + GenericRepository
public class StockReceiptService(IUnitOfWork unitOfWork) : IStockReceiptService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork; // Inject UnitOfWork để truy cập repository và quản lý transaction

    // Ghi nhận nhập thêm hàng (POST) - tạo phiếu nhập mới
    public async Task<StockReceiptDtoResponse> CreateStockReceiptAsync(CreateStockReceiptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);                   // Kiểm tra request không được null

        var variantRepository = _unitOfWork.Repository<ProductVariant>(); // Lấy repository kiểm tra variant tồn tại
        var variantExists = await variantRepository.ExistsAsync(
            v => v.VariantId == request.VariantId);                   // Kiểm tra VariantId có tồn tại trong DB không

        if (!variantExists)
        {
            throw new KeyNotFoundException($"ProductVariant with id {request.VariantId} not found."); // Throw exception nếu variant không tồn tại
        }

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>(); // Lấy repository cho StockReceipt entity (dùng full namespace tránh xung đột)

        // Tạo entity mới từ request DTO
        var stockReceipt = new RepositoryLayer.Entities.StockReceipt
        {
            VariantId = request.VariantId,                            // Gán VariantId từ request
            QuantityReceived = request.QuantityReceived,              // Gán số lượng nhập
            ReceivedDate = DateTime.UtcNow,                           // Gán thời gian nhận hàng = thời gian hiện tại (UTC)
            Note = request.Note?.Trim()                               // Gán ghi chú (trim khoảng trắng, nullable)
        };

        await repository.AddAsync(stockReceipt);                      // Thêm vào DbContext (chưa lưu DB)
        await _unitOfWork.SaveChangesAsync(cancellationToken);        // Lưu vào DB → ReceiptId được tự động gán bởi EF Core

        return MapToDto(stockReceipt);                                // Trả về DTO của phiếu nhập vừa tạo
    }

    // Xem lịch sử nhập hàng có phân trang, lọc và khoảng thời gian (copy logic pagination từ PolicyService)
    public async Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,                                               // Lọc theo variant cụ thể
        int? staffId,                                                 // Lọc theo nhân viên nhập
        DateTime? fromDate,                                           // Lọc từ ngày
        DateTime? toDate,                                             // Lọc đến ngày
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);         // Kiểm tra paginationRequest không được null

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>(); // Lấy repository cho StockReceipt entity

        var pagedReceipts = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,                     // Truyền thông tin phân trang (page, pageSize)
            filter: receipt =>
                (!variantId.HasValue || receipt.VariantId == variantId.Value) &&     // Lọc theo variantId nếu có
                (!staffId.HasValue || receipt.StaffId == staffId.Value) &&           // Lọc theo staffId nếu có
                (!fromDate.HasValue || receipt.ReceivedDate >= fromDate.Value) &&    // Lọc từ ngày nếu có
                (!toDate.HasValue || receipt.ReceivedDate <= toDate.Value),          // Lọc đến ngày nếu có
            orderBy: query => query.OrderByDescending(receipt => receipt.ReceivedDate), // Sắp xếp theo ngày nhận hàng mới nhất trước
            tracked: false,                                           // Không cần track vì chỉ đọc dữ liệu
            cancellationToken: cancellationToken);

        var items = pagedReceipts.Items
            .Select(MapToListDto)                                     // Chuyển đổi từ Entity sang DTO danh sách
            .ToList();

        return PagedResult<StockReceiptListDtoResponse>.Create(       // Tạo PagedResult mới với DTO items (copy pattern từ PolicyService)
            items,
            pagedReceipts.Page,
            pagedReceipts.PageSize,
            pagedReceipts.TotalItems);
    }

    // Lấy chi tiết stock receipt theo ReceiptId
    public async Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>(); // Lấy repository cho StockReceipt entity

        var stockReceipt = await repository.GetByIdAsync(receiptId);  // Tìm stock receipt theo primary key (ReceiptId)

        return stockReceipt is null ? null : MapToDto(stockReceipt);  // Trả về null nếu không tìm thấy, hoặc DTO nếu có
    }

    // Helper method: chuyển đổi từ Entity sang DTO chi tiết (dùng cho GET by ID và POST response)
    private static StockReceiptDtoResponse MapToDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        return new StockReceiptDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,                       // Map ReceiptId
            VariantId = stockReceipt.VariantId,                       // Map VariantId
            QuantityReceived = stockReceipt.QuantityReceived,         // Map QuantityReceived
            Note = stockReceipt.Note                                  // Map Note (nullable)
        };
    }

    // Helper method: chuyển đổi từ Entity sang DTO danh sách (dùng cho GET list, thêm ReceivedDate)
    private static StockReceiptListDtoResponse MapToListDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        return new StockReceiptListDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,                       // Map ReceiptId
            VariantId = stockReceipt.VariantId,                       // Map VariantId
            QuantityReceived = stockReceipt.QuantityReceived,         // Map QuantityReceived
            ReceivedDate = stockReceipt.ReceivedDate                  // Map ReceivedDate
        };
    }
}
