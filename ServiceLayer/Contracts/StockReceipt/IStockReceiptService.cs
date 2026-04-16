using RepositoryLayer.Common;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Contracts.StockReceipt;

// Interface định nghĩa các nghiệp vụ quản lý Stock Receipt (phiếu nhập hàng)
public interface IStockReceiptService
{
    // Ghi nhận nhập thêm hàng, trả về DTO chứa thông tin phiếu nhập vừa tạo
    Task<StockReceiptDtoResponse> CreateStockReceiptAsync(CreateStockReceiptRequest request, CancellationToken cancellationToken = default);

    // Xem lịch sử nhập hàng có phân trang, lọc theo variantId/staffId và khoảng thời gian
    Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,                               // Lọc theo variant cụ thể (optional)
        int? staffId,                                 // Lọc theo nhân viên nhập (optional)
        DateTime? fromDate,                           // Lọc từ ngày (optional)
        DateTime? toDate,                             // Lọc đến ngày (optional)
        CancellationToken cancellationToken = default);

    // Lấy chi tiết 1 stock receipt, trả về null nếu không tìm thấy
    Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default);
}
