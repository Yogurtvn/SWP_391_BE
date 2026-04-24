using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Product;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Product.Request;
using ServiceLayer.DTOs.Product.Response;
using ServiceLayer.DTOs.ProductImage.Response;
using ServiceLayer.DTOs.ProductVariant.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.ProductManagement;

public class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        GetProductsRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var productType = ParseProductTypeOrNull(request.ProductType);
        var normalizedSearch = NormalizeText(request.Search);
        var normalizedColor = NormalizeText(request.Color);
        var normalizedSize = NormalizeText(request.Size);
        var normalizedFrameType = NormalizeText(request.FrameType);
        var (sortBy, sortDescending) = NormalizeProductSort(request.SortBy, request.SortOrder);

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice.Value > request.MaxPrice.Value)
        {
            throw CreateInvalidQueryException("minPrice", "minPrice must be less than or equal to maxPrice");
        }

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var paginationRequest = new PaginationRequest(request.Page, request.PageSize);

        var pagedProducts = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: product =>
                (includeInactive || product.IsActive) &&
                (!request.CategoryId.HasValue || product.CategoryId == request.CategoryId.Value) &&
                (!productType.HasValue || product.ProductType == productType.Value) &&
                (!request.PrescriptionCompatible.HasValue || product.PrescriptionCompatible == request.PrescriptionCompatible.Value) &&
                (normalizedSearch == null
                    || product.ProductName.Contains(normalizedSearch)
                    || (product.Description != null && product.Description.Contains(normalizedSearch))) &&
                (normalizedColor == null
                    || product.Variants.Any(variant =>
                        (includeInactive || variant.IsActive)
                        && variant.Color != null
                        && variant.Color.Contains(normalizedColor))) &&
                (normalizedSize == null
                    || product.Variants.Any(variant =>
                        (includeInactive || variant.IsActive)
                        && variant.Size != null
                        && variant.Size.Contains(normalizedSize))) &&
                (normalizedFrameType == null
                    || product.Variants.Any(variant =>
                        (includeInactive || variant.IsActive)
                        && variant.FrameType != null
                        && variant.FrameType.Contains(normalizedFrameType))) &&
                (!request.MinPrice.HasValue
                    || ((product.Variants
                            .Where(variant => includeInactive || variant.IsActive)
                            .Select(variant => (decimal?)variant.Price)
                            .Min() ?? product.BasePrice) >= request.MinPrice.Value)) &&
                (!request.MaxPrice.HasValue
                    || ((product.Variants
                            .Where(variant => includeInactive || variant.IsActive)
                            .Select(variant => (decimal?)variant.Price)
                            .Min() ?? product.BasePrice) <= request.MaxPrice.Value)),
            orderBy: query => ApplyProductOrdering(query, sortBy, sortDescending, includeInactive),
            includeProperties: "Variants.Inventory,Variants.Promotion,Images",
            tracked: false,
            cancellationToken: cancellationToken);

        var now = DateTime.UtcNow;
        var items = pagedProducts.Items
            .Select(product => MapToListItem(product, includeInactive, now))
            .ToList();

        return PagedResult<ProductListItemResponse>.Create(
            items,
            pagedProducts.Page,
            pagedProducts.PageSize,
            pagedProducts.TotalItems);
    }

    public async Task<ProductDetailResponse?> GetProductByIdAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var product = await repository.GetFirstOrDefaultAsync(
            productEntity => productEntity.ProductId == productId && (includeInactive || productEntity.IsActive),
            includeProperties: "Variants.Inventory,Variants.Promotion,Images",
            tracked: false);

        if (product is null)
        {
            return null;
        }

        return MapToDetailResponse(product, includeInactive, DateTime.UtcNow);
    }

    public async Task<ProductIdResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var productType = ParseRequiredProductType(request.ProductType);
        await EnsureCategoryExistsAsync(request.CategoryId);

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var product = new RepositoryLayer.Entities.Product
        {
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            ProductType = productType,
            PrescriptionCompatible = request.PrescriptionCompatible,
            Description = NormalizeNullableText(request.Description),
            BasePrice = request.BasePrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductIdResponse
        {
            ProductId = product.ProductId
        };
    }

    public async Task<MessageResponse> UpdateProductAsync(int productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var product = await repository.GetByIdAsync(productId);

        if (product is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }

        var productType = ParseRequiredProductType(request.ProductType);
        await EnsureCategoryExistsAsync(request.CategoryId);

        product.ProductName = request.ProductName.Trim();
        product.CategoryId = request.CategoryId;
        product.ProductType = productType;
        product.PrescriptionCompatible = request.PrescriptionCompatible;
        product.Description = NormalizeNullableText(request.Description);
        product.BasePrice = request.BasePrice;

        repository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Product updated"
        };
    }

    public async Task<MessageResponse> UpdateProductStatusAsync(int productId, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.IsActive.HasValue)
        {
            throw CreateValidationException("isActive", "isActive is required");
        }

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var product = await repository.GetByIdAsync(productId);

        if (product is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }

        product.IsActive = request.IsActive.Value;
        repository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Product status updated"
        };
    }

    public async Task<MessageResponse> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var productRepository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var cartItemRepository = _unitOfWork.Repository<CartItem>();
        var orderItemRepository = _unitOfWork.Repository<OrderItem>();
        var stockReceiptRepository = _unitOfWork.Repository<StockReceipt>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var imageRepository = _unitOfWork.Repository<ProductImage>();
        var variantRepository = _unitOfWork.Repository<ProductVariant>();

        var product = await productRepository.GetFirstOrDefaultAsync(
            productEntity => productEntity.ProductId == productId,
            includeProperties: "Variants.Inventory,Images");

        if (product is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }

        var variantIds = product.Variants.Select(variant => variant.VariantId).ToList();

        if (variantIds.Count > 0
            && (await cartItemRepository.ExistsAsync(item => variantIds.Contains(item.VariantId))
                || await orderItemRepository.ExistsAsync(item => variantIds.Contains(item.VariantId))
                || await stockReceiptRepository.ExistsAsync(receipt => variantIds.Contains(receipt.VariantId))))
        {
            throw new ApiException(
                (int)HttpStatusCode.Conflict,
                "PRODUCT_DELETE_NOT_ALLOWED",
                "Product cannot be deleted because it has variants that are already in use");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (product.Images.Count > 0)
            {
                imageRepository.RemoveRange(product.Images);
            }

            var inventories = product.Variants
                .Where(variant => variant.Inventory is not null)
                .Select(variant => variant.Inventory!)
                .ToList();

            if (inventories.Count > 0)
            {
                inventoryRepository.RemoveRange(inventories);
            }

            if (product.Variants.Count > 0)
            {
                variantRepository.RemoveRange(product.Variants);
            }

            // TODO: revisit physical delete strategy once soft-delete vs hard-delete is finalized in API_SPEC.md.
            productRepository.Remove(product);
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
            Message = "Product deleted"
        };
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        var categoryExists = await categoryRepository.ExistsAsync(category => category.CategoryId == categoryId);

        if (!categoryExists)
        {
            throw CreateValidationException("categoryId", "categoryId must reference an existing category");
        }
    }

    private static ProductListItemResponse MapToListItem(
        RepositoryLayer.Entities.Product product,
        bool includeInactive,
        DateTime currentTime)
    {
        var visibleVariants = GetVisibleVariants(product, includeInactive)
            .ToList();
        var orderedImages = product.Images
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.ImageId)
            .ToList();
        var primaryImage = orderedImages.FirstOrDefault(image => image.IsPrimary) ?? orderedImages.FirstOrDefault();

        return new ProductListItemResponse
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            ProductType = ToApiEnum(product.ProductType),
            BasePrice = ResolveDisplayPrice(product, includeInactive),
            ThumbnailUrl = primaryImage?.ImageUrl,
            IsActive = product.IsActive,
            IsAvailable = visibleVariants.Any(variant => (variant.Inventory?.Quantity ?? 0) > 0),
            IsReadyAvailable = visibleVariants.Any(variant => (variant.Inventory?.Quantity ?? 0) > 0),
            IsPreOrderAllowed = visibleVariants.Any(variant => variant.Inventory?.IsPreOrderAllowed == true),
            Variants = visibleVariants
                .OrderBy(variant => variant.VariantId)
                .Select(variant => MapVariantToListItem(variant, currentTime))
                .ToList()
        };
    }

    private static ProductDetailResponse MapToDetailResponse(
        RepositoryLayer.Entities.Product product,
        bool includeInactive,
        DateTime currentTime)
    {
        var variants = GetVisibleVariants(product, includeInactive)
            .OrderBy(variant => variant.VariantId)
            .Select(variant => MapVariantToListItem(variant, currentTime))
            .ToList();
        var images = product.Images
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.ImageId)
            .Select(MapImage)
            .ToList();

        return new ProductDetailResponse
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            CategoryId = product.CategoryId,
            ProductType = ToApiEnum(product.ProductType),
            Description = product.Description,
            BasePrice = product.BasePrice,
            IsActive = product.IsActive,
            PrescriptionCompatible = product.PrescriptionCompatible,
            Variants = variants,
            Images = images
        };
    }

    private static ProductVariantListItemResponse MapVariantToListItem(ProductVariant variant, DateTime currentTime)
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

    private static ProductImageResponse MapImage(ProductImage image)
    {
        return new ProductImageResponse
        {
            ImageId = image.ImageId,
            ImageUrl = image.ImageUrl,
            DisplayOrder = image.DisplayOrder,
            IsPrimary = image.IsPrimary
        };
    }

    private static IEnumerable<ProductVariant> GetVisibleVariants(RepositoryLayer.Entities.Product product, bool includeInactive)
    {
        return includeInactive
            ? product.Variants
            : product.Variants.Where(variant => variant.IsActive);
    }

    private static decimal ResolveDisplayPrice(RepositoryLayer.Entities.Product product, bool includeInactive)
    {
        return GetVisibleVariants(product, includeInactive)
            .Select(variant => variant.Price)
            .DefaultIfEmpty(product.BasePrice)
            .Min();
    }

    private static IOrderedQueryable<RepositoryLayer.Entities.Product> ApplyProductOrdering(
        IQueryable<RepositoryLayer.Entities.Product> query,
        string sortBy,
        bool sortDescending,
        bool includeInactive)
    {
        return sortBy switch
        {
            "price" when sortDescending => query.OrderByDescending(product =>
                    product.Variants
                        .Where(variant => includeInactive || variant.IsActive)
                        .Select(variant => (decimal?)variant.Price)
                        .Min() ?? product.BasePrice)
                .ThenByDescending(product => product.ProductId),
            "price" => query.OrderBy(product =>
                    product.Variants
                        .Where(variant => includeInactive || variant.IsActive)
                        .Select(variant => (decimal?)variant.Price)
                        .Min() ?? product.BasePrice)
                .ThenBy(product => product.ProductId),
            _ when sortDescending => query.OrderByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.ProductId),
            _ => query.OrderBy(product => product.CreatedAt)
                .ThenBy(product => product.ProductId)
        };
    }

    private static ProductType ParseRequiredProductType(string rawProductType)
    {
        var normalizedValue = NormalizeText(rawProductType);

        return normalizedValue is not null
               && Enum.TryParse<ProductType>(normalizedValue, ignoreCase: true, out var parsedValue)
            ? parsedValue
            : throw CreateValidationException("productType", "productType is invalid");
    }

    private static ProductType? ParseProductTypeOrNull(string? rawProductType)
    {
        var normalizedValue = NormalizeText(rawProductType);

        if (normalizedValue is null)
        {
            return null;
        }

        return Enum.TryParse<ProductType>(normalizedValue, ignoreCase: true, out var parsedValue)
            ? parsedValue
            : throw CreateInvalidQueryException("productType", "productType is invalid");
    }

    private static (string SortBy, bool SortDescending) NormalizeProductSort(string? rawSortBy, string? rawSortOrder)
    {
        var sortBy = NormalizeText(rawSortBy)?.ToLowerInvariant() ?? "newest";
        var sortOrder = NormalizeText(rawSortOrder)?.ToLowerInvariant();

        if (sortBy == "mostpopular")
        {
            throw new ApiException(
                (int)HttpStatusCode.BadRequest,
                "INVALID_QUERY",
                "sortBy 'mostPopular' is not available because its rule is still TBD in API_SPEC.md");
        }

        if (sortBy is not ("price" or "newest"))
        {
            throw CreateInvalidQueryException("sortBy", "sortBy is invalid");
        }

        return sortOrder switch
        {
            null => (sortBy, sortBy != "price"),
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

    private static string? NormalizeNullableText(string? value)
    {
        return NormalizeText(value);
    }

    private static string ToApiEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return value.ToString().ToLowerInvariant();
    }

    private static ApiException CreateValidationException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "VALIDATION_ERROR",
            "Invalid product data",
            new { field, issue });
    }

    private static ApiException CreateInvalidQueryException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_QUERY",
            "Invalid product query",
            new { field, issue });
    }
}
