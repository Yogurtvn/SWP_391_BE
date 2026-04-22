using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Notifications;
using ServiceLayer.Contracts.ProductVariant;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductVariant.Request;
using ServiceLayer.DTOs.ProductVariant.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.ProductVariantManagement;

public class ProductVariantService(
    IUnitOfWork unitOfWork,
    IPreOrderBackInStockNotificationService backInStockNotificationService) : IProductVariantService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPreOrderBackInStockNotificationService _backInStockNotificationService = backInStockNotificationService;

    public async Task<PagedResult<ProductVariantListItemResponse>> GetVariantsByProductAsync(
        int productId,
        GetProductVariantsRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var productRepository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var productExists = await productRepository.ExistsAsync(
            product => product.ProductId == productId && (includeInactive || product.IsActive));

        if (!productExists)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }

        var (sortBy, sortDescending) = NormalizeSort(request.SortBy, request.SortOrder);
        var normalizedColor = NormalizeText(request.Color);
        var normalizedSize = NormalizeText(request.Size);
        var normalizedFrameType = NormalizeText(request.FrameType);
        var effectiveIsActive = includeInactive ? request.IsActive : true;
        var repository = _unitOfWork.Repository<ProductVariant>();
        var paginationRequest = new PaginationRequest(request.Page, request.PageSize);

        var pagedVariants = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: variant =>
                variant.ProductId == productId &&
                (!effectiveIsActive.HasValue || variant.IsActive == effectiveIsActive.Value) &&
                (normalizedColor == null || (variant.Color != null && variant.Color.Contains(normalizedColor))) &&
                (normalizedSize == null || (variant.Size != null && variant.Size.Contains(normalizedSize))) &&
                (normalizedFrameType == null || (variant.FrameType != null && variant.FrameType.Contains(normalizedFrameType))),
            orderBy: query => ApplyOrdering(query, sortBy, sortDescending),
            includeProperties: "Inventory,Promotion",
            tracked: false,
            cancellationToken: cancellationToken);
        var now = DateTime.UtcNow;

        var items = pagedVariants.Items
            .Select(variant => MapToListItem(variant, now))
            .ToList();

        return PagedResult<ProductVariantListItemResponse>.Create(
            items,
            pagedVariants.Page,
            pagedVariants.PageSize,
            pagedVariants.TotalItems);
    }

    public async Task<ProductVariantDetailResponse?> GetVariantByIdAsync(
        int variantId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductVariant>();
        var variant = await repository.GetFirstOrDefaultAsync(
            variantEntity =>
                variantEntity.VariantId == variantId
                && (includeInactive || (variantEntity.IsActive && variantEntity.Product.IsActive)),
            includeProperties: "Inventory,Product,Promotion",
            tracked: false);

        return variant is null ? null : MapToDetail(variant, DateTime.UtcNow);
    }

    public async Task<ProductVariantIdResponse> CreateVariantAsync(
        int productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId);
        await EnsureSkuUniqueAsync(request.Sku);

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = request.Sku.Trim(),
            FrameType = NormalizeText(request.FrameType),
            Size = NormalizeText(request.Size),
            Color = NormalizeText(request.Color),
            Price = request.Price,
            IsActive = true
        };

        var inventory = CreateInventoryEntity(request.Quantity, request.IsPreOrderAllowed, request.ExpectedRestockDate, request.PreOrderNote);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await variantRepository.AddAsync(variant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            inventory.VariantId = variant.VariantId;
            await inventoryRepository.AddAsync(inventory);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new ProductVariantIdResponse
        {
            VariantId = variant.VariantId
        };
    }

    public async Task<MessageResponse> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var variant = await variantRepository.GetFirstOrDefaultAsync(
            variantEntity => variantEntity.VariantId == variantId,
            includeProperties: "Inventory");

        if (variant is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "VARIANT_NOT_FOUND", "Variant not found");
        }

        await EnsureSkuUniqueAsync(request.Sku, variantId);

        variant.Sku = request.Sku.Trim();
        variant.FrameType = NormalizeText(request.FrameType);
        variant.Size = NormalizeText(request.Size);
        variant.Color = NormalizeText(request.Color);
        variant.Price = request.Price;
        var previousQuantity = variant.Inventory?.Quantity ?? 0;

        var inventory = variant.Inventory;
        if (inventory is null)
        {
            inventory = CreateInventoryEntity(request.Quantity, request.IsPreOrderAllowed, request.ExpectedRestockDate, request.PreOrderNote);
            inventory.VariantId = variant.VariantId;
            await inventoryRepository.AddAsync(inventory);
        }
        else
        {
            ApplyInventoryChanges(inventory, request.Quantity, request.IsPreOrderAllowed, request.ExpectedRestockDate, request.PreOrderNote);
            inventoryRepository.Update(inventory);
        }

        variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var currentQuantity = inventory.Quantity;

        await _backInStockNotificationService.HandleStockChangeAsync(
            variantId,
            previousQuantity,
            currentQuantity,
            source: "variant:update",
            cancellationToken);

        return new MessageResponse
        {
            Message = "Variant updated"
        };
    }

    public async Task<MessageResponse> UpdateVariantStatusAsync(
        int variantId,
        UpdateProductVariantStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsActive.HasValue)
        {
            throw CreateValidationException("isActive", "isActive is required");
        }

        var repository = _unitOfWork.Repository<ProductVariant>();
        var variant = await repository.GetByIdAsync(variantId);

        if (variant is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "VARIANT_NOT_FOUND", "Variant not found");
        }

        variant.IsActive = request.IsActive.Value;
        repository.Update(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Variant status updated"
        };
    }

    public async Task<MessageResponse> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var cartItemRepository = _unitOfWork.Repository<CartItem>();
        var orderItemRepository = _unitOfWork.Repository<OrderItem>();
        var stockReceiptRepository = _unitOfWork.Repository<StockReceipt>();

        var variant = await variantRepository.GetFirstOrDefaultAsync(
            variantEntity => variantEntity.VariantId == variantId,
            includeProperties: "Inventory");

        if (variant is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "VARIANT_NOT_FOUND", "Variant not found");
        }

        if (await cartItemRepository.ExistsAsync(item => item.VariantId == variantId)
            || await orderItemRepository.ExistsAsync(item => item.VariantId == variantId)
            || await stockReceiptRepository.ExistsAsync(receipt => receipt.VariantId == variantId))
        {
            throw new ApiException(
                (int)HttpStatusCode.Conflict,
                "VARIANT_DELETE_NOT_ALLOWED",
                "Variant cannot be deleted because it is already in use");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (variant.Inventory is not null)
            {
                inventoryRepository.Remove(variant.Inventory);
            }

            variantRepository.Remove(variant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new MessageResponse
        {
            Message = "Variant deleted"
        };
    }

    private async Task EnsureProductExistsAsync(int productId)
    {
        var productRepository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var productExists = await productRepository.ExistsAsync(product => product.ProductId == productId);

        if (!productExists)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }
    }

    private async Task EnsureSkuUniqueAsync(string sku, int? excludedVariantId = null)
    {
        var normalizedSku = sku.Trim();
        var repository = _unitOfWork.Repository<ProductVariant>();
        var skuExists = await repository.ExistsAsync(
            variant => variant.Sku == normalizedSku && (!excludedVariantId.HasValue || variant.VariantId != excludedVariantId.Value));

        if (skuExists)
        {
            throw CreateValidationException("sku", "sku must be unique");
        }
    }

    private static ProductVariantListItemResponse MapToListItem(ProductVariant variant, DateTime currentTime)
    {
        var pricing = PromotionPricingHelper.Calculate(variant, currentTime);

        return new ProductVariantListItemResponse
        {
            VariantId = variant.VariantId,
            Sku = variant.Sku,
            Color = variant.Color,
            Size = variant.Size,
            Price = variant.Price,
            OriginalPrice = pricing.OriginalPrice,
            DiscountPercent = pricing.DiscountPercent,
            DiscountAmount = pricing.DiscountAmount,
            FinalPrice = pricing.FinalPrice,
            Quantity = variant.Inventory?.Quantity ?? 0,
            IsReadyAvailable = (variant.Inventory?.Quantity ?? 0) > 0,
            IsPreOrderAllowed = variant.Inventory?.IsPreOrderAllowed ?? false,
            ExpectedRestockDate = variant.Inventory?.ExpectedRestockDate,
            PreOrderNote = variant.Inventory?.PreOrderNote
        };
    }

    private static ProductVariantDetailResponse MapToDetail(ProductVariant variant, DateTime currentTime)
    {
        var pricing = PromotionPricingHelper.Calculate(variant, currentTime);

        return new ProductVariantDetailResponse
        {
            VariantId = variant.VariantId,
            Sku = variant.Sku,
            Color = variant.Color,
            Size = variant.Size,
            FrameType = variant.FrameType,
            Price = variant.Price,
            OriginalPrice = pricing.OriginalPrice,
            DiscountPercent = pricing.DiscountPercent,
            DiscountAmount = pricing.DiscountAmount,
            FinalPrice = pricing.FinalPrice,
            Quantity = variant.Inventory?.Quantity ?? 0,
            IsReadyAvailable = (variant.Inventory?.Quantity ?? 0) > 0,
            IsPreOrderAllowed = variant.Inventory?.IsPreOrderAllowed ?? false,
            ExpectedRestockDate = variant.Inventory?.ExpectedRestockDate,
            PreOrderNote = variant.Inventory?.PreOrderNote
        };
    }

    private static Inventory CreateInventoryEntity(
        int quantity,
        bool isPreOrderAllowed,
        DateTime? expectedRestockDate,
        string? preOrderNote)
    {
        var inventory = new Inventory();
        ApplyInventoryChanges(inventory, quantity, isPreOrderAllowed, expectedRestockDate, preOrderNote);
        return inventory;
    }

    private static void ApplyInventoryChanges(
        Inventory inventory,
        int quantity,
        bool isPreOrderAllowed,
        DateTime? expectedRestockDate,
        string? preOrderNote)
    {
        inventory.Quantity = quantity;
        inventory.IsPreOrderAllowed = isPreOrderAllowed;
        inventory.ExpectedRestockDate = isPreOrderAllowed ? expectedRestockDate : null;
        inventory.PreOrderNote = isPreOrderAllowed ? NormalizeText(preOrderNote) : null;
    }

    private static IOrderedQueryable<ProductVariant> ApplyOrdering(
        IQueryable<ProductVariant> query,
        string sortBy,
        bool sortDescending)
    {
        return sortBy switch
        {
            "price" when sortDescending => query.OrderByDescending(variant => variant.Price).ThenByDescending(variant => variant.VariantId),
            "price" => query.OrderBy(variant => variant.Price).ThenBy(variant => variant.VariantId),
            "sku" when sortDescending => query.OrderByDescending(variant => variant.Sku).ThenByDescending(variant => variant.VariantId),
            "sku" => query.OrderBy(variant => variant.Sku).ThenBy(variant => variant.VariantId),
            "color" when sortDescending => query.OrderByDescending(variant => variant.Color).ThenByDescending(variant => variant.VariantId),
            "color" => query.OrderBy(variant => variant.Color).ThenBy(variant => variant.VariantId),
            "size" when sortDescending => query.OrderByDescending(variant => variant.Size).ThenByDescending(variant => variant.VariantId),
            "size" => query.OrderBy(variant => variant.Size).ThenBy(variant => variant.VariantId),
            _ when sortDescending => query.OrderByDescending(variant => variant.VariantId),
            _ => query.OrderBy(variant => variant.VariantId)
        };
    }

    private static (string SortBy, bool SortDescending) NormalizeSort(string? rawSortBy, string? rawSortOrder)
    {
        var sortBy = NormalizeText(rawSortBy)?.ToLowerInvariant() ?? "variantid";
        var sortOrder = NormalizeText(rawSortOrder)?.ToLowerInvariant();

        if (sortBy is not ("variantid" or "price" or "sku" or "color" or "size"))
        {
            throw CreateInvalidQueryException("sortBy", "sortBy is invalid");
        }

        return sortOrder switch
        {
            null => (sortBy, false),
            "asc" => (sortBy, false),
            "desc" => (sortBy, true),
            _ => throw CreateInvalidQueryException("sortOrder", "sortOrder must be 'asc' or 'desc'")
        };
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static ApiException CreateValidationException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "VALIDATION_ERROR",
            "Invalid variant data",
            new { field, issue });
    }

    private static ApiException CreateInvalidQueryException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_QUERY",
            "Invalid variant query",
            new { field, issue });
    }
}
