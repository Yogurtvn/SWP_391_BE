using RepositoryLayer.Common;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Contracts.StockReceipt;

// Interface Ä‘á»‹nh nghÄ©a cÃ¡c nghiá»‡p vá»¥ quáº£n lÃ½ Stock Receipt (phiáº¿u nháº­p hÃ ng)
public interface IStockReceiptService
{
    // Ghi nháº­n nháº­p thÃªm hÃ ng, tráº£ vá» DTO chá»©a thÃ´ng tin phiáº¿u nháº­p vá»«a táº¡o
    Task<StockReceiptDtoResponse> CreateStockReceiptAsync(
        CreateStockReceiptRequest request,
        int? staffId = null,
        CancellationToken cancellationToken = default);

    // Xem lá»‹ch sá»­ nháº­p hÃ ng cÃ³ phÃ¢n trang, lá»c theo variantId/staffId vÃ  khoáº£ng thá»i gian
    Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,                               // Lá»c theo variant cá»¥ thá»ƒ (optional)
        int? staffId,                                 // Lá»c theo nhÃ¢n viÃªn nháº­p (optional)
        DateTime? fromDate,                           // Lá»c tá»« ngÃ y (optional)
        DateTime? toDate,                             // Lá»c Ä‘áº¿n ngÃ y (optional)
        CancellationToken cancellationToken = default);

    // Láº¥y chi tiáº¿t 1 stock receipt, tráº£ vá» null náº¿u khÃ´ng tÃ¬m tháº¥y
    Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default);
}
