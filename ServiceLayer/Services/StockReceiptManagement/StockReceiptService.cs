using Microsoft.Extensions.Logging;
using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Services.StockReceiptManagement;

public class StockReceiptService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<StockReceiptService> logger) : IStockReceiptService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<StockReceiptService> _logger = logger;
    private const string BackInStockSubject = "product is back in stock";
    private const string BackInStockBody = "Your preorder item is now back in stock and your order will be processed soon.";

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

            var previousQuantity = inventory.Quantity;
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

            if (previousQuantity <= 0 && inventory.Quantity > 0)
            {
                await NotifyAwaitingPreOrdersAsync(request.VariantId, cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Skipping preorder notification because variant stock did not transition from out-of-stock to in-stock. VariantId: {VariantId}, PreviousQuantity: {PreviousQuantity}, CurrentQuantity: {CurrentQuantity}",
                    request.VariantId,
                    previousQuantity,
                    inventory.Quantity);
            }

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

    private async Task NotifyAwaitingPreOrdersAsync(int variantId, CancellationToken cancellationToken)
    {
        try
        {
            var orderRepository = _unitOfWork.Repository<Order>();
            var preorderOrders = await orderRepository.FindAsync(
                filter: order =>
                    order.OrderType == OrderType.PreOrder &&
                    order.OrderStatus == OrderStatus.AwaitingStock &&
                    order.OrderItems.Any(item => item.VariantId == variantId),
                includeProperties: "User",
                tracked: false);

            var recipientEmails = preorderOrders
                .Select(order => order.User.Email?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipientEmails.Count == 0)
            {
                _logger.LogInformation(
                    "No awaiting preorder recipients found after stock receipt. VariantId: {VariantId}",
                    variantId);
                return;
            }

            foreach (var recipientEmail in recipientEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        recipientEmail!,
                        BackInStockSubject,
                        BackInStockBody,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send preorder back-in-stock notification. VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        variantId,
                        recipientEmail);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to collect awaiting preorder recipients after stock receipt. VariantId: {VariantId}",
                variantId);
        }
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
