using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Cart;
using ServiceLayer.DTOs.Cart.Request;
using ServiceLayer.DTOs.Cart.Response;
using ServiceLayer.DTOs.Common;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.CartManagement;

public class CartService(IUnitOfWork unitOfWork) : ICartService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<CartDetailResponse> GetMyCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartAsync(
            userId,
            tracked: true,
            includeProperties: "CartItems.Variant,CartItems.CartPrescriptionDetail.LensType");

        if (cart is null)
        {
            cart = await CreateCartAsync(userId, DateTime.UtcNow, cancellationToken);
        }

        return MapCart(cart);
    }

    public async Task<MessageResponse> ClearMyCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetCartAsync(
            userId,
            tracked: true,
            includeProperties: "CartItems.CartPrescriptionDetail");

        if (cart is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "CART_NOT_FOUND", "Cart not found");
        }

        var detailRepository = _unitOfWork.Repository<CartPrescriptionDetail>();
        var itemRepository = _unitOfWork.Repository<CartItem>();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var prescriptionDetails = cart.CartItems
                .Where(item => item.CartPrescriptionDetail is not null)
                .Select(item => item.CartPrescriptionDetail!)
                .ToList();

            if (prescriptionDetails.Count > 0)
            {
                detailRepository.RemoveRange(prescriptionDetails);
            }

            if (cart.CartItems.Count > 0)
            {
                itemRepository.RemoveRange(cart.CartItems);
            }

            cart.UpdatedAt = DateTime.UtcNow;
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
            Message = "Cart cleared"
        };
    }

    public async Task<StandardCartItemCreatedResponse> AddStandardItemAsync(
        int userId,
        AddStandardCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variantId = request.VariantId ?? throw CreateInvalidCartItemException("variantId", "variantId is required");
        var quantity = request.Quantity ?? throw CreateInvalidCartItemException("quantity", "quantity is required");

        if (variantId <= 0)
        {
            throw CreateInvalidCartItemException("variantId", "variantId must be greater than 0");
        }

        if (quantity <= 0)
        {
            throw CreateInvalidCartItemException("quantity", "quantity must be greater than 0");
        }

        var orderType = ParseStandardOrderType(request.OrderType);
        var variant = await GetActiveVariantAsync(variantId);

        if (variant is null)
        {
            throw CreateInvalidCartItemException("variantId", "variantId must reference an existing active variant");
        }

        ValidateStandardOrderRequest(variant, orderType, quantity);

        var now = DateTime.UtcNow;
        var cartItemRepository = _unitOfWork.Repository<CartItem>();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var pricing = PromotionPricingHelper.Calculate(variant, now);

            var cart = await GetOrCreateCartAsync(userId, now, cancellationToken);
            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                VariantId = variant.VariantId,
                ItemType = CartItemType.Standard,
                OrderType = orderType,
                Quantity = quantity,
                SelectedColor = NormalizeText(variant.Color),
                OriginalUnitPrice = pricing.OriginalPrice,
                DiscountPercent = pricing.DiscountPercent,
                DiscountAmount = pricing.DiscountAmount,
                FinalUnitPrice = pricing.FinalPrice,
                UnitPrice = pricing.FinalPrice,
                TotalPrice = pricing.FinalPrice * quantity,
                CreatedAt = now,
                UpdatedAt = now
            };

            cart.UpdatedAt = now;
            await cartItemRepository.AddAsync(cartItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new StandardCartItemCreatedResponse
            {
                CartItemId = cartItem.CartItemId,
                ItemType = ToApiCartItemType(cartItem.ItemType),
                OrderType = ToApiOrderType(cartItem.OrderType),
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.TotalPrice
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StandardCartItemUpdatedResponse> UpdateStandardItemAsync(
        int userId,
        int cartItemId,
        UpdateStandardCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quantity = request.Quantity ?? throw CreateInvalidCartItemException("quantity", "quantity is required", "Invalid cart item data");

        if (quantity <= 0)
        {
            throw CreateInvalidCartItemException("quantity", "quantity must be greater than 0", "Invalid cart item data");
        }

        var cartItem = await GetCartItemAsync(
            userId,
            cartItemId,
            CartItemType.Standard,
            tracked: true,
            includeProperties: "Cart,Variant.Product,Variant.Inventory,Variant.Promotion");

        if (cartItem is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "CART_ITEM_NOT_FOUND", "Cart item not found");
        }

        if (cartItem.Variant is null)
        {
            throw CreateInvalidCartItemException("variantId", "variantId must reference an existing active variant", "Invalid cart item data");
        }

        ValidateStandardOrderRequest(cartItem.Variant, cartItem.OrderType, quantity);

        var now = DateTime.UtcNow;
        var pricing = PromotionPricingHelper.Calculate(cartItem.Variant, now);

        cartItem.Quantity = quantity;
        cartItem.OriginalUnitPrice = pricing.OriginalPrice;
        cartItem.DiscountPercent = pricing.DiscountPercent;
        cartItem.DiscountAmount = pricing.DiscountAmount;
        cartItem.FinalUnitPrice = pricing.FinalPrice;
        cartItem.UnitPrice = pricing.FinalPrice;
        cartItem.TotalPrice = pricing.FinalPrice * quantity;
        cartItem.UpdatedAt = now;
        cartItem.Cart.UpdatedAt = cartItem.UpdatedAt;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StandardCartItemUpdatedResponse
        {
            Message = "Cart item updated",
            TotalPrice = cartItem.TotalPrice
        };
    }

    public async Task<MessageResponse> DeleteStandardItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await GetCartItemAsync(
            userId,
            cartItemId,
            CartItemType.Standard,
            tracked: true,
            includeProperties: "Cart,CartPrescriptionDetail");

        if (cartItem is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "CART_ITEM_NOT_FOUND", "Cart item not found");
        }

        await DeleteCartItemAsync(cartItem, cancellationToken);

        return new MessageResponse
        {
            Message = "Cart item removed"
        };
    }

    public async Task<PrescriptionCartItemCreatedResponse> AddPrescriptionItemAsync(
        int userId,
        UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var preparedRequest = PreparePrescriptionRequest(request);
        var variant = await GetPrescriptionCompatibleVariantAsync(preparedRequest.VariantId);

        if (variant is null)
        {
            throw CreateInvalidPrescriptionException(
                "variantId",
                "variantId must reference an active prescription-compatible frame variant");
        }

        var lensType = await GetActiveLensTypeAsync(preparedRequest.LensTypeId);

        if (lensType is null)
        {
            throw CreateInvalidPrescriptionException(
                "lensTypeId",
                "lensTypeId must reference an existing active lens type");
        }

        var now = DateTime.UtcNow;
        var pricing = PromotionPricingHelper.Calculate(variant, now);

        var prescriptionPricing = CalculatePrescriptionPricing(pricing.FinalPrice, lensType.Price, preparedRequest.Quantity);
        var cartItemRepository = _unitOfWork.Repository<CartItem>();
        var detailRepository = _unitOfWork.Repository<CartPrescriptionDetail>();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var cart = await GetOrCreateCartAsync(userId, now, cancellationToken);
            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                VariantId = variant.VariantId,
                ItemType = CartItemType.PrescriptionConfigured,
                OrderType = OrderType.Prescription,
                Quantity = preparedRequest.Quantity,
                SelectedColor = NormalizeText(variant.Color),
                OriginalUnitPrice = pricing.OriginalPrice,
                DiscountPercent = pricing.DiscountPercent,
                DiscountAmount = pricing.DiscountAmount,
                FinalUnitPrice = pricing.FinalPrice,
                UnitPrice = pricing.FinalPrice,
                TotalPrice = prescriptionPricing.TotalPrice,
                CreatedAt = now,
                UpdatedAt = now
            };

            cart.UpdatedAt = now;
            await cartItemRepository.AddAsync(cartItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var prescriptionDetail = CreatePrescriptionDetail(cartItem.CartItemId, lensType, preparedRequest, prescriptionPricing, now);
            await detailRepository.AddAsync(prescriptionDetail);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new PrescriptionCartItemCreatedResponse
            {
                CartItemId = cartItem.CartItemId,
                ItemType = ToApiCartItemType(cartItem.ItemType),
                OrderType = ToApiOrderType(cartItem.OrderType),
                FramePrice = cartItem.UnitPrice,
                LensPrice = prescriptionPricing.LensPricePerUnit,
                TotalPrice = cartItem.TotalPrice
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MessageResponse> UpdatePrescriptionItemAsync(
        int userId,
        int cartItemId,
        UpsertPrescriptionCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cartItem = await GetCartItemAsync(
            userId,
            cartItemId,
            CartItemType.PrescriptionConfigured,
            tracked: true,
            includeProperties: "Cart,CartPrescriptionDetail");

        if (cartItem is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "CART_ITEM_NOT_FOUND", "Prescription cart item not found");
        }

        var preparedRequest = PreparePrescriptionRequest(request);
        var variant = await GetPrescriptionCompatibleVariantAsync(preparedRequest.VariantId);

        if (variant is null)
        {
            throw CreateInvalidPrescriptionException(
                "variantId",
                "variantId must reference an active prescription-compatible frame variant");
        }

        var lensType = await GetActiveLensTypeAsync(preparedRequest.LensTypeId);

        if (lensType is null)
        {
            throw CreateInvalidPrescriptionException(
                "lensTypeId",
                "lensTypeId must reference an existing active lens type");
        }

        var now = DateTime.UtcNow;
        var pricing = PromotionPricingHelper.Calculate(variant, now);

        var prescriptionPricing = CalculatePrescriptionPricing(pricing.FinalPrice, lensType.Price, preparedRequest.Quantity);
        var detailRepository = _unitOfWork.Repository<CartPrescriptionDetail>();

        cartItem.VariantId = variant.VariantId;
        cartItem.Quantity = preparedRequest.Quantity;
        cartItem.SelectedColor = NormalizeText(variant.Color);
        cartItem.OrderType = OrderType.Prescription;
        cartItem.OriginalUnitPrice = pricing.OriginalPrice;
        cartItem.DiscountPercent = pricing.DiscountPercent;
        cartItem.DiscountAmount = pricing.DiscountAmount;
        cartItem.FinalUnitPrice = pricing.FinalPrice;
        cartItem.UnitPrice = pricing.FinalPrice;
        cartItem.TotalPrice = prescriptionPricing.TotalPrice;
        cartItem.UpdatedAt = now;
        cartItem.Cart.UpdatedAt = now;

        if (cartItem.CartPrescriptionDetail is null)
        {
            cartItem.CartPrescriptionDetail = CreatePrescriptionDetail(cartItem.CartItemId, lensType, preparedRequest, prescriptionPricing, now);
            await detailRepository.AddAsync(cartItem.CartPrescriptionDetail);
        }
        else
        {
            ApplyPrescriptionDetail(cartItem.CartPrescriptionDetail, lensType, preparedRequest, prescriptionPricing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Prescription cart item updated"
        };
    }

    public async Task<MessageResponse> DeletePrescriptionItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await GetCartItemAsync(
            userId,
            cartItemId,
            CartItemType.PrescriptionConfigured,
            tracked: true,
            includeProperties: "Cart,CartPrescriptionDetail");

        if (cartItem is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "CART_ITEM_NOT_FOUND", "Prescription cart item not found");
        }

        await DeleteCartItemAsync(cartItem, cancellationToken);

        return new MessageResponse
        {
            Message = "Prescription cart item removed"
        };
    }

    private async Task DeleteCartItemAsync(
        CartItem cartItem,
        CancellationToken cancellationToken)
    {
        var cartItemRepository = _unitOfWork.Repository<CartItem>();
        var detailRepository = _unitOfWork.Repository<CartPrescriptionDetail>();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (cartItem.CartPrescriptionDetail is not null)
            {
                detailRepository.Remove(cartItem.CartPrescriptionDetail);
            }

            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            cartItemRepository.Remove(cartItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Cart?> GetCartAsync(
        int userId,
        bool tracked,
        string includeProperties = "")
    {
        var repository = _unitOfWork.Repository<Cart>();
        return await repository.GetFirstOrDefaultAsync(
            cart => cart.UserId == userId,
            includeProperties: includeProperties,
            tracked: tracked);
    }

    private async Task<Cart> GetOrCreateCartAsync(
        int userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Cart>();
        var cart = await GetCartAsync(userId, tracked: true);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<Cart> CreateCartAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Cart>();
        var cart = new Cart
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<CartItem?> GetCartItemAsync(
        int userId,
        int cartItemId,
        CartItemType itemType,
        bool tracked,
        string includeProperties = "")
    {
        var repository = _unitOfWork.Repository<CartItem>();
        return await repository.GetFirstOrDefaultAsync(
            item => item.CartItemId == cartItemId
                && item.ItemType == itemType
                && item.Cart.UserId == userId,
            includeProperties: includeProperties,
            tracked: tracked);
    }

    private async Task<ProductVariant?> GetActiveVariantAsync(int variantId)
    {
        var repository = _unitOfWork.Repository<ProductVariant>();
        return await repository.GetFirstOrDefaultAsync(
            variant => variant.VariantId == variantId
                && variant.IsActive
                && variant.Product.IsActive,
            includeProperties: "Product,Inventory,Promotion",
            tracked: false);
    }

    private async Task<ProductVariant?> GetPrescriptionCompatibleVariantAsync(int variantId)
    {
        var repository = _unitOfWork.Repository<ProductVariant>();
        return await repository.GetFirstOrDefaultAsync(
            variant => variant.VariantId == variantId
                && variant.IsActive
                && variant.Product.IsActive
                && variant.Product.ProductType == ProductType.Frame
                && variant.Product.PrescriptionCompatible,
            includeProperties: "Product,Promotion",
            tracked: false);
    }

    private async Task<LensType?> GetActiveLensTypeAsync(int lensTypeId)
    {
        var repository = _unitOfWork.Repository<LensType>();
        return await repository.GetFirstOrDefaultAsync(
            lensType => lensType.LensTypeId == lensTypeId && lensType.IsActive,
            tracked: false);
    }

    private static CartDetailResponse MapCart(Cart cart)
    {
        var items = cart.CartItems
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.CartItemId)
            .Select(MapCartItem)
            .ToList();

        return new CartDetailResponse
        {
            CartId = cart.CartId,
            Items = items,
            SubTotal = cart.CartItems.Sum(item => item.TotalPrice)
        };
    }

    private static CartItemResponse MapCartItem(CartItem cartItem)
    {
        return new CartItemResponse
        {
            CartItemId = cartItem.CartItemId,
            VariantId = cartItem.VariantId,
            ItemType = ToApiCartItemType(cartItem.ItemType),
            OrderType = ToApiOrderType(cartItem.OrderType),
            Quantity = cartItem.Quantity,
            UnitPrice = cartItem.UnitPrice,
            OriginalUnitPrice = cartItem.OriginalUnitPrice,
            DiscountPercent = cartItem.DiscountPercent,
            DiscountAmount = cartItem.DiscountAmount,
            FinalUnitPrice = cartItem.FinalUnitPrice,
            TotalPrice = cartItem.TotalPrice,
            Prescription = cartItem.CartPrescriptionDetail is null
                ? null
                : new PrescriptionCartItemDetailResponse
                {
                    LensTypeId = cartItem.CartPrescriptionDetail.LensTypeId,
                    LensCode = cartItem.CartPrescriptionDetail.LensTypeCode,
                    LensName = cartItem.CartPrescriptionDetail.LensType?.LensName,
                    LensMaterial = cartItem.CartPrescriptionDetail.LensMaterial,
                    Coatings = DeserializeCoatings(cartItem.CartPrescriptionDetail.Coatings),
                    LensPrice = cartItem.CartPrescriptionDetail.TotalLensPrice,
                    RightEye = new PrescriptionEyeResponse
                    {
                        Sph = cartItem.CartPrescriptionDetail.SphRight,
                        Cyl = cartItem.CartPrescriptionDetail.CylRight,
                        Axis = cartItem.CartPrescriptionDetail.AxisRight
                    },
                    LeftEye = new PrescriptionEyeResponse
                    {
                        Sph = cartItem.CartPrescriptionDetail.SphLeft,
                        Cyl = cartItem.CartPrescriptionDetail.CylLeft,
                        Axis = cartItem.CartPrescriptionDetail.AxisLeft
                    },
                    Pd = cartItem.CartPrescriptionDetail.Pd,
                    Notes = cartItem.CartPrescriptionDetail.Notes,
                    PrescriptionImageUrl = cartItem.CartPrescriptionDetail.PrescriptionImage
                }
        };
    }

    private static PreparedPrescriptionRequest PreparePrescriptionRequest(UpsertPrescriptionCartItemRequest request)
    {
        var variantId = request.VariantId ?? throw CreateInvalidPrescriptionException("variantId", "variantId is required");
        var quantity = request.Quantity ?? throw CreateInvalidPrescriptionException("quantity", "quantity is required");
        var lensTypeId = request.LensTypeId ?? throw CreateInvalidPrescriptionException("lensTypeId", "lensTypeId is required");

        if (variantId <= 0)
        {
            throw CreateInvalidPrescriptionException("variantId", "variantId must be greater than 0");
        }

        if (quantity <= 0)
        {
            throw CreateInvalidPrescriptionException("quantity", "quantity must be greater than 0");
        }

        if (lensTypeId <= 0)
        {
            throw CreateInvalidPrescriptionException("lensTypeId", "lensTypeId must be greater than 0");
        }

        var rightEye = request.RightEye;
        var leftEye = request.LeftEye;

        if (rightEye?.Sph is null
            || rightEye.Cyl is null
            || rightEye.Axis is null
            || leftEye?.Sph is null
            || leftEye.Cyl is null
            || leftEye.Axis is null
            || request.Pd is null)
        {
            throw CreateInvalidPrescriptionException(
                "manualPrescription",
                "Manual prescription input is required",
                "Manual prescription input is required");
        }

        ValidateAxis(rightEye.Axis.Value, "rightEye.axis");
        ValidateAxis(leftEye.Axis.Value, "leftEye.axis");

        if (request.Pd.Value <= 0)
        {
            throw CreateInvalidPrescriptionException("pd", "pd must be greater than 0");
        }

        var lensMaterial = NormalizeOptionalText(request.LensMaterial, 50, "lensMaterial");
        var notes = NormalizeOptionalText(request.Notes, 255, "notes");
        var prescriptionImageUrl = NormalizeOptionalText(request.PrescriptionImageUrl, 500, "prescriptionImageUrl");
        var coatings = NormalizeCoatings(request.Coatings);

        return new PreparedPrescriptionRequest(
            VariantId: variantId,
            Quantity: quantity,
            LensTypeId: lensTypeId,
            LensMaterial: lensMaterial,
            Coatings: coatings,
            RightSph: rightEye.Sph.Value,
            RightCyl: rightEye.Cyl.Value,
            RightAxis: rightEye.Axis.Value,
            LeftSph: leftEye.Sph.Value,
            LeftCyl: leftEye.Cyl.Value,
            LeftAxis: leftEye.Axis.Value,
            Pd: request.Pd.Value,
            Notes: notes,
            PrescriptionImageUrl: prescriptionImageUrl);
    }

    private static CartPrescriptionDetail CreatePrescriptionDetail(
        int cartItemId,
        LensType lensType,
        PreparedPrescriptionRequest request,
        PrescriptionPricing pricing,
        DateTime now)
    {
        var detail = new CartPrescriptionDetail
        {
            CartItemId = cartItemId,
            CreatedAt = now
        };

        ApplyPrescriptionDetail(detail, lensType, request, pricing);
        return detail;
    }

    private static void ApplyPrescriptionDetail(
        CartPrescriptionDetail detail,
        LensType lensType,
        PreparedPrescriptionRequest request,
        PrescriptionPricing pricing)
    {
        detail.LensTypeId = lensType.LensTypeId;
        detail.LensTypeCode = lensType.LensCode;
        detail.LensMaterial = request.LensMaterial;
        detail.Coatings = request.Coatings;
        detail.LensBasePrice = pricing.LensBasePrice;
        detail.CoatingPrice = pricing.CoatingPricePerUnit;
        detail.TotalLensPrice = pricing.LensPricePerUnit;
        detail.SphRight = request.RightSph;
        detail.CylRight = request.RightCyl;
        detail.AxisRight = request.RightAxis;
        detail.SphLeft = request.LeftSph;
        detail.CylLeft = request.LeftCyl;
        detail.AxisLeft = request.LeftAxis;
        detail.Pd = request.Pd;
        detail.PrescriptionImage = request.PrescriptionImageUrl;
        detail.Notes = request.Notes;
    }

    private static PrescriptionPricing CalculatePrescriptionPricing(decimal framePrice, decimal lensBasePrice, int quantity)
    {
        // TODO: introduce coating-specific pricing once the spec defines chargeable coating options.
        var coatingPricePerUnit = 0m;
        var lensPricePerUnit = lensBasePrice + coatingPricePerUnit;

        return new PrescriptionPricing(
            FramePricePerUnit: framePrice,
            LensBasePrice: lensBasePrice,
            CoatingPricePerUnit: coatingPricePerUnit,
            LensPricePerUnit: lensPricePerUnit,
            TotalPrice: (framePrice + lensPricePerUnit) * quantity);
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string field)
    {
        var normalizedValue = NormalizeText(value);

        if (normalizedValue is not null && normalizedValue.Length > maxLength)
        {
            throw CreateInvalidPrescriptionException(field, $"{field} must not exceed {maxLength} characters");
        }

        return normalizedValue;
    }

    private static string? NormalizeCoatings(List<string>? coatings)
    {
        if (coatings is null || coatings.Count == 0)
        {
            return null;
        }

        var normalizedValues = coatings
            .Select(NormalizeText)
            .Where(value => value is not null)
            .Cast<string>()
            .ToList();

        if (normalizedValues.Count == 0)
        {
            return null;
        }

        var serializedCoatings = string.Join(",", normalizedValues);

        if (serializedCoatings.Length > 500)
        {
            throw CreateInvalidPrescriptionException("coatings", "coatings must not exceed 500 characters");
        }

        return serializedCoatings;
    }

    private static List<string> DeserializeCoatings(string? coatings)
    {
        return string.IsNullOrWhiteSpace(coatings)
            ? []
            : coatings
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
    }

    private static OrderType ParseStandardOrderType(string? rawOrderType)
    {
        var normalizedValue = NormalizeText(rawOrderType)?.Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalizedValue switch
        {
            "ready" => OrderType.Ready,
            "preorder" => OrderType.PreOrder,
            _ => throw CreateInvalidCartItemException("orderType", "orderType must be 'ready' or 'preOrder'")
        };
    }

    private static void ValidateStandardOrderRequest(ProductVariant variant, OrderType orderType, int quantity)
    {
        if (variant.Inventory is null)
        {
            throw CreateInvalidCartItemException(
                "variantId",
                "variantId must reference a variant with inventory configured");
        }

        if (orderType == OrderType.Ready && variant.Inventory.Quantity < quantity)
        {
            throw CreateInvalidCartItemException(
                "quantity",
                $"Only {variant.Inventory.Quantity} item(s) are currently available for ready order");
        }

        if (orderType == OrderType.PreOrder && !variant.Inventory.IsPreOrderAllowed)
        {
            throw CreateInvalidCartItemException(
                "orderType",
                "preOrder is not allowed for this variant");
        }
    }

    private static void ValidateAxis(int axis, string field)
    {
        if (axis is < 0 or > 180)
        {
            throw CreateInvalidPrescriptionException(field, $"{field} must be between 0 and 180");
        }
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static string ToApiCartItemType(CartItemType itemType)
    {
        return itemType switch
        {
            CartItemType.Standard => "standard",
            CartItemType.PrescriptionConfigured => "prescriptionConfigured",
            _ => itemType.ToString()
        };
    }

    private static string ToApiOrderType(OrderType orderType)
    {
        return orderType switch
        {
            OrderType.Ready => "ready",
            OrderType.PreOrder => "preOrder",
            OrderType.Prescription => "prescription",
            _ => orderType.ToString()
        };
    }

    private static ApiException CreateInvalidCartItemException(string field, string issue, string message = "Cannot add item to cart")
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_CART_ITEM",
            message,
            new { field, issue });
    }

    private static ApiException CreateInvalidPrescriptionException(
        string field,
        string issue,
        string message = "Invalid prescription input")
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_PRESCRIPTION_INPUT",
            message,
            new { field, issue });
    }

    private sealed record PreparedPrescriptionRequest(
        int VariantId,
        int Quantity,
        int LensTypeId,
        string? LensMaterial,
        string? Coatings,
        decimal RightSph,
        decimal RightCyl,
        int RightAxis,
        decimal LeftSph,
        decimal LeftCyl,
        int LeftAxis,
        decimal Pd,
        string? Notes,
        string? PrescriptionImageUrl);

    private sealed record PrescriptionPricing(
        decimal FramePricePerUnit,
        decimal LensBasePrice,
        decimal CoatingPricePerUnit,
        decimal LensPricePerUnit,
        decimal TotalPrice);
}
