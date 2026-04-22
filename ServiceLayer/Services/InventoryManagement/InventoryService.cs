using Microsoft.Extensions.Logging;
using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;
using ServiceLayer.Exceptions;
using System.Net;
using InventoryEntity = RepositoryLayer.Entities.Inventory;

namespace ServiceLayer.Services.InventoryManagement;

public class InventoryService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<InventoryService> logger) : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<InventoryService> _logger = logger;
    private const string BackInStockSubject = "product is back in stock";
    private const string BackInStockBody = "Your preorder item is now back in stock and your order will be processed soon.";

    public async Task<PagedResult<InventoryListDtoResponse>> GetInventoriesAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? productId,
        bool? isPreOrderAllowed,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var repository = _unitOfWork.Repository<InventoryEntity>();
        var normalizedSearch = NormalizeText(search);
        var (normalizedSortBy, sortDescending) = NormalizeSort(sortBy, sortOrder);

        var pagedInventories = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: inventory =>
                (!variantId.HasValue || inventory.VariantId == variantId.Value) &&
                (!productId.HasValue || inventory.Variant.ProductId == productId.Value) &&
                (!isPreOrderAllowed.HasValue || inventory.IsPreOrderAllowed == isPreOrderAllowed.Value) &&
                (normalizedSearch == null
                    || inventory.Variant.Sku.Contains(normalizedSearch)
                    || (inventory.Variant.Color != null && inventory.Variant.Color.Contains(normalizedSearch))
                    || (inventory.Variant.Size != null && inventory.Variant.Size.Contains(normalizedSearch))
                    || (inventory.Variant.FrameType != null && inventory.Variant.FrameType.Contains(normalizedSearch))
                    || inventory.Variant.Product.ProductName.Contains(normalizedSearch)),
            orderBy: query => ApplyOrdering(query, normalizedSortBy, sortDescending),
            includeProperties: "Variant.Product",
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedInventories.Items
            .Select(MapToListDto)
            .ToList();

        return PagedResult<InventoryListDtoResponse>.Create(
            items,
            pagedInventories.Page,
            pagedInventories.PageSize,
            pagedInventories.TotalItems);
    }

    public async Task<InventoryDtoResponse?> GetInventoryByVariantIdAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        return inventory is null ? null : MapToDto(inventory);
    }

    public async Task<bool> UpdateInventoryAsync(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        if (inventory is null)
        {
            return false;
        }

        var previousQuantity = inventory.Quantity;
        inventory.Quantity = request.Quantity;
        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;
        inventory.ExpectedRestockDate = request.ExpectedRestockDate;
        inventory.PreOrderNote = request.PreOrderNote?.Trim();

        repository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousQuantity <= 0 && inventory.Quantity > 0)
        {
            await NotifyAwaitingPreOrdersAsync(variantId, cancellationToken);
        }

        return true;
    }

    public async Task<bool> UpdatePreOrderAsync(int variantId, UpdatePreOrderRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        if (inventory is null)
        {
            return false;
        }

        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;
        inventory.ExpectedRestockDate = request.ExpectedRestockDate;
        inventory.PreOrderNote = request.PreOrderNote?.Trim();

        repository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static InventoryDtoResponse MapToDto(InventoryEntity inventory)
    {
        return new InventoryDtoResponse
        {
            VariantId = inventory.VariantId,
            Quantity = inventory.Quantity,
            IsReadyAvailable = inventory.Quantity > 0,
            IsPreOrderAllowed = inventory.IsPreOrderAllowed,
            ExpectedRestockDate = inventory.ExpectedRestockDate,
            PreOrderNote = inventory.PreOrderNote
        };
    }

    private static InventoryListDtoResponse MapToListDto(InventoryEntity inventory)
    {
        return new InventoryListDtoResponse
        {
            VariantId = inventory.VariantId,
            Quantity = inventory.Quantity,
            IsReadyAvailable = inventory.Quantity > 0,
            IsPreOrderAllowed = inventory.IsPreOrderAllowed,
            ExpectedRestockDate = inventory.ExpectedRestockDate,
            PreOrderNote = inventory.PreOrderNote
        };
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static (string SortBy, bool SortDescending) NormalizeSort(string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = NormalizeText(sortBy)?.ToLowerInvariant() ?? "variantid";
        var normalizedSortOrder = NormalizeText(sortOrder)?.ToLowerInvariant();

        if (normalizedSortBy is not ("variantid" or "quantity" or "expectedrestockdate"))
        {
            throw CreateInvalidQueryException("sortBy", "sortBy is invalid");
        }

        return normalizedSortOrder switch
        {
            null => (normalizedSortBy, false),
            "asc" => (normalizedSortBy, false),
            "desc" => (normalizedSortBy, true),
            _ => throw CreateInvalidQueryException("sortOrder", "sortOrder must be 'asc' or 'desc'")
        };
    }

    private static IOrderedQueryable<InventoryEntity> ApplyOrdering(
        IQueryable<InventoryEntity> query,
        string sortBy,
        bool sortDescending)
    {
        return sortBy switch
        {
            "quantity" when sortDescending => query.OrderByDescending(inventory => inventory.Quantity)
                .ThenByDescending(inventory => inventory.VariantId),
            "quantity" => query.OrderBy(inventory => inventory.Quantity)
                .ThenBy(inventory => inventory.VariantId),
            "expectedrestockdate" when sortDescending => query.OrderByDescending(inventory => inventory.ExpectedRestockDate)
                .ThenByDescending(inventory => inventory.VariantId),
            "expectedrestockdate" => query.OrderBy(inventory => inventory.ExpectedRestockDate)
                .ThenBy(inventory => inventory.VariantId),
            _ when sortDescending => query.OrderByDescending(inventory => inventory.VariantId),
            _ => query.OrderBy(inventory => inventory.VariantId)
        };
    }

    private static ApiException CreateInvalidQueryException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_QUERY",
            "Invalid inventory query",
            new { field, issue });
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
                    "No awaiting preorder recipients found after inventory update. VariantId: {VariantId}",
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
                        "Failed to send preorder back-in-stock notification after inventory update. VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        variantId,
                        recipientEmail);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to collect awaiting preorder recipients after inventory update. VariantId: {VariantId}",
                variantId);
        }
    }
}
