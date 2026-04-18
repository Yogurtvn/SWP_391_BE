using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Services.StockReceiptManagement;

public class StockReceiptService(IUnitOfWork unitOfWork) : IStockReceiptService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<StockReceiptDtoResponse> CreateStockReceiptAsync(
        CreateStockReceiptRequest request,
        int staffUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var receiptRepository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();
        var now = DateTime.UtcNow;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var variant = await variantRepository.GetFirstOrDefaultAsync(
                variant => variant.VariantId == request.VariantId,
                includeProperties: "Inventory",
                tracked: true);

            if (variant is null)
            {
                throw new KeyNotFoundException($"ProductVariant with id {request.VariantId} not found.");
            }

            var inventory = variant.Inventory;

            if (inventory is null)
            {
                inventory = new Inventory
                {
                    VariantId = variant.VariantId,
                    Quantity = 0,
                    IsPreOrderAllowed = false
                };

                await inventoryRepository.AddAsync(inventory);
                variant.Inventory = inventory;
            }

            inventory.Quantity += request.QuantityReceived;

            var stockReceipt = new RepositoryLayer.Entities.StockReceipt
            {
                VariantId = request.VariantId,
                QuantityReceived = request.QuantityReceived,
                ReceivedDate = now,
                StaffId = staffUserId,
                Note = request.Note?.Trim()
            };

            await receiptRepository.AddAsync(stockReceipt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapToDto(stockReceipt);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? staffId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();

        var pagedReceipts = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: receipt =>
                (!variantId.HasValue || receipt.VariantId == variantId.Value) &&
                (!staffId.HasValue || receipt.StaffId == staffId.Value) &&
                (!fromDate.HasValue || receipt.ReceivedDate >= fromDate.Value) &&
                (!toDate.HasValue || receipt.ReceivedDate <= toDate.Value),
            orderBy: query => query.OrderByDescending(receipt => receipt.ReceivedDate),
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedReceipts.Items
            .Select(MapToListDto)
            .ToList();

        return PagedResult<StockReceiptListDtoResponse>.Create(
            items,
            pagedReceipts.Page,
            pagedReceipts.PageSize,
            pagedReceipts.TotalItems);
    }

    public async Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();
        var stockReceipt = await repository.GetByIdAsync(receiptId);

        return stockReceipt is null ? null : MapToDto(stockReceipt);
    }

    private static StockReceiptDtoResponse MapToDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        return new StockReceiptDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,
            VariantId = stockReceipt.VariantId,
            QuantityReceived = stockReceipt.QuantityReceived,
            Note = stockReceipt.Note
        };
    }

    private static StockReceiptListDtoResponse MapToListDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        return new StockReceiptListDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,
            VariantId = stockReceipt.VariantId,
            QuantityReceived = stockReceipt.QuantityReceived,
            ReceivedDate = stockReceipt.ReceivedDate
        };
    }
}
