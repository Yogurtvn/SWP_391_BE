using RepositoryLayer.Common;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Contracts.StockReceipt;

public interface IStockReceiptService
{
    Task<StockReceiptDtoResponse> CreateStockReceiptAsync(
        CreateStockReceiptRequest request,
        int staffUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? staffId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default);
}
