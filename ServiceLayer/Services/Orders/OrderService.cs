using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Orders;
using ServiceLayer.DTOs.Payment.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.Orders;

public class OrderService(
    IUnitOfWork unitOfWork,
    OnlineEyewearDbContext dbContext,
    IPaymentService paymentService,
    IPrescriptionPricingService prescriptionPricingService) : IOrderService
{
    private static readonly HashSet<OrderStatus> CancellableStatuses =
    [
        OrderStatus.Pending,
        OrderStatus.Confirmed,
        OrderStatus.AwaitingStock
    ];

    // TODO: align the transition matrix once API_SPEC.md defines the exact orderStatus rules.
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedStatusTransitions = new()
    {
        [OrderStatus.Pending] =
        [
            OrderStatus.Confirmed,
            OrderStatus.AwaitingStock,
            OrderStatus.Cancelled
        ],
        [OrderStatus.Confirmed] =
        [
            OrderStatus.Processing,
            OrderStatus.AwaitingStock,
            OrderStatus.Cancelled
        ],
        [OrderStatus.AwaitingStock] =
        [
            OrderStatus.Confirmed,
            OrderStatus.Cancelled
        ],
        [OrderStatus.Processing] =
        [
            OrderStatus.Shipped,
            OrderStatus.Cancelled
        ],
        [OrderStatus.Shipped] =
        [
            OrderStatus.Completed
        ],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IPrescriptionPricingService _prescriptionPricingService = prescriptionPricingService;

    public async Task<CheckoutOrderResponse> CheckoutOrderAsync(
        int userId,
        CheckoutOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var receiverName = NormalizeRequiredText(request.ReceiverName, "receiverName");
        var receiverPhone = NormalizeRequiredText(request.ReceiverPhone, "receiverPhone");
        var shippingAddress = NormalizeRequiredText(request.ShippingAddress, "shippingAddress");
        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var cartItemIds = PrepareCartItemIds(request.CartItemIds);
        var now = DateTime.UtcNow;

        var cartItems = await _dbContext.CartItems
            .Include(item => item.Cart)
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.Product)
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.Inventory)
            .Include(item => item.Variant)
                .ThenInclude(variant => variant.Promotion)
            .Include(item => item.CartPrescriptionDetail)
                .ThenInclude(detail => detail!.LensType)
            .Where(item => cartItemIds.Contains(item.CartItemId) && item.Cart.UserId == userId)
            .OrderBy(item => item.CartItemId)
            .ToListAsync(cancellationToken);

        if (cartItems.Count != cartItemIds.Count)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        var orderType = ResolveCheckoutOrderType(cartItems);
        var orderItems = BuildCheckoutOrderItems(userId, cartItems, orderType, now);
        var result = await CreateOrderAsync(
            userId,
            orderType,
            receiverName,
            receiverPhone,
            shippingAddress,
            paymentMethod,
            orderItems,
            createdByUserId: userId,
            requestPayOsInitialization: paymentMethod == PaymentMethod.PayOS,
            cartItemsToRemove: cartItems,
            cancellationToken: cancellationToken);

        return new CheckoutOrderResponse
        {
            OrderId = result.Order.OrderId,
            TotalAmount = result.Order.TotalAmount,
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(result.Order.OrderStatus),
            Payment = MapCheckoutPayment(result.Payment, result.PaymentAction)
        };
    }

    public async Task<BuyNowOrderResponse> BuyNowAsync(
        int userId,
        BuyNowOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.VariantId <= 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_REQUEST", "variantId must be greater than 0");
        }

        if (request.Quantity <= 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_REQUEST", "quantity must be greater than 0");
        }

        var receiverName = NormalizeRequiredText(request.ReceiverName, "receiverName");
        var receiverPhone = NormalizeRequiredText(request.ReceiverPhone, "receiverPhone");
        var shippingAddress = NormalizeRequiredText(request.ShippingAddress, "shippingAddress");
        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var now = DateTime.UtcNow;

        var variant = await _dbContext.ProductVariants
            .Include(item => item.Product)
            .Include(item => item.Inventory)
            .Include(item => item.Promotion)
            .FirstOrDefaultAsync(
                item => item.VariantId == request.VariantId,
                cancellationToken);

        if (variant is null || !variant.IsActive || !variant.Product.IsActive)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_REQUEST", "Selected variant is not available");
        }

        if (variant.Inventory is null || variant.Inventory.Quantity < request.Quantity)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "OUT_OF_STOCK", "Selected variant is out of stock");
        }

        var pricing = PromotionPricingHelper.Calculate(variant, now);

        var result = await CreateOrderAsync(
            userId,
            OrderType.Ready,
            receiverName,
            receiverPhone,
            shippingAddress,
            paymentMethod,
            [
                new OrderCreationItem
                {
                    Variant = variant,
                    Quantity = request.Quantity,
                    SelectedColor = NormalizeText(variant.Color),
                    OriginalUnitPrice = pricing.OriginalPrice,
                    DiscountPercent = pricing.DiscountPercent,
                    DiscountAmount = pricing.DiscountAmount,
                    FinalUnitPrice = pricing.FinalPrice,
                    UnitPrice = pricing.FinalPrice,
                    PromotionNameSnapshot = pricing.PromotionName,
                    LineTotal = pricing.FinalPrice * request.Quantity,
                    ReserveInventory = true
                }
            ],
            createdByUserId: userId,
            requestPayOsInitialization: paymentMethod == PaymentMethod.PayOS,
            cancellationToken: cancellationToken);

        return new BuyNowOrderResponse
        {
            OrderId = result.Order.OrderId,
            OrderType = ApiEnumMapper.ToApiOrderType(result.Order.OrderType),
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(result.Order.OrderStatus),
            Payment = MapCheckoutPayment(result.Payment, result.PaymentAction)
        };
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetOrdersAsync(
        int currentUserId,
        bool canAccessAllOrders,
        GetOrdersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Include(order => order.Payments)
            .AsSplitQuery()
            .AsQueryable();

        if (!canAccessAllOrders)
        {
            query = query.Where(order => order.UserId == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(request.OrderType))
        {
            var orderType = ParseOrderType(request.OrderType);
            query = query.Where(order => order.OrderType == orderType);
        }

        if (!string.IsNullOrWhiteSpace(request.OrderStatus))
        {
            var orderStatus = ParseOrderStatus(request.OrderStatus);
            query = query.Where(order => order.OrderStatus == orderStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.ShippingStatus))
        {
            var shippingStatus = ParseShippingStatus(request.ShippingStatus);
            query = query.Where(order => order.ShippingStatus == shippingStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            var paymentStatus = ParsePaymentStatus(request.PaymentStatus);
            query = query.Where(order => order.Payments.Any(payment => payment.PaymentStatus == paymentStatus));
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;
            query = query.Where(order => order.CreatedAt >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDateExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(order => order.CreatedAt < toDateExclusive);
        }

        var page = Math.Max(request.Page, PaginationRequest.DefaultPage);
        var pageSize = request.PageSize < 1
            ? PaginationRequest.DefaultPageSize
            : Math.Min(request.PageSize, PaginationRequest.MaxPageSize);
        var sortDescending = ParseSortOrder(request.SortOrder);

        query = ApplyOrderSorting(query, request.SortBy, sortDescending);

        var totalItems = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<OrderSummaryResponse>.Create(
            orders.Select(MapOrderSummary).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<OrderDetailResponse?> GetOrderByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderQuery(currentUserId, canAccessAllOrders, tracked: false)
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.Product)
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.LensType)
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.Prescription)
            .Include(current => current.Payments)
                .ThenInclude(payment => payment.PaymentHistories)
            .Include(current => current.OrderStatusHistories)
                .ThenInclude(history => history.UpdatedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        return order is null ? null : MapOrderDetail(order);
    }

    public async Task<OrderCancelResponse> CancelOrderAsync(
        int userId,
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _dbContext.Orders
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.Inventory)
            .Include(current => current.Payments)
                .ThenInclude(payment => payment.PaymentHistories)
            .Include(current => current.OrderStatusHistories)
            .FirstOrDefaultAsync(
                current => current.OrderId == orderId && current.UserId == userId,
                cancellationToken);

        if (order is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "ORDER_NOT_FOUND", "Order not found");
        }

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            throw CreateApiException(HttpStatusCode.Conflict, "ORDER_CANNOT_BE_CANCELLED", "Order cannot be cancelled at current status");
        }

        if (!CancellableStatuses.Contains(order.OrderStatus))
        {
            throw CreateApiException(HttpStatusCode.Conflict, "ORDER_CANNOT_BE_CANCELLED", "Order cannot be cancelled at current status");
        }

        if (order.Payments.Any(payment => payment.PaymentStatus == PaymentStatus.Completed))
        {
            throw CreateApiException(HttpStatusCode.Conflict, "ORDER_CANNOT_BE_CANCELLED", "Order cannot be cancelled at current status");
        }

        var note = NormalizeText(request.Reason);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await CancelOrderInternalAsync(order, userId, note ?? "Order cancelled by customer.", cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new OrderCancelResponse
        {
            Message = "Order cancelled",
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(order.OrderStatus)
        };
    }

    public async Task<MessageResponse> AssignStaffAsync(
        int currentUserId,
        int orderId,
        AssignOrderStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var staffId = request.StaffId.GetValueOrDefault();

        if (staffId <= 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "STAFF_NOT_FOUND", "Staff not found");
        }

        var staff = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.UserId == staffId && user.Role == UserRole.Staff && user.IsActive,
                cancellationToken);

        if (staff is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "STAFF_NOT_FOUND", "Staff not found");
        }

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "ORDER_NOT_FOUND", "Order not found");
        }

        order.StaffId = staffId;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Staff assigned"
        };
    }

    public async Task<OrderItemsResponse?> GetOrderItemsAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderQuery(currentUserId, canAccessAllOrders, tracked: false)
            .Include(current => current.OrderItems)
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderItemsResponse
        {
            Items = order.OrderItems
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderItemListItemResponse
                {
                    OrderItemId = item.OrderItemId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    OriginalUnitPrice = item.OriginalUnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    FinalUnitPrice = item.FinalUnitPrice,
                    PromotionNameSnapshot = item.PromotionNameSnapshot,
                    LensTypeId = item.LensTypeId,
                    LensPrice = item.LensPrice,
                    PrescriptionId = item.PrescriptionId
                })
                .ToList()
        };
    }

    public async Task<OrderItemDetailResponse?> GetOrderItemByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        int orderItemId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderQuery(currentUserId, canAccessAllOrders, tracked: false)
            .Include(current => current.OrderItems)
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var orderItem = order.OrderItems.FirstOrDefault(item => item.OrderItemId == orderItemId);

        if (orderItem is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "ORDER_ITEM_NOT_FOUND", "Order item not found");
        }

        return new OrderItemDetailResponse
        {
            OrderItemId = orderItem.OrderItemId,
            VariantId = orderItem.VariantId,
            Quantity = orderItem.Quantity,
            SelectedColor = orderItem.SelectedColor,
            TotalPrice = (orderItem.UnitPrice + (orderItem.LensPrice ?? 0m)) * orderItem.Quantity,
            OriginalUnitPrice = orderItem.OriginalUnitPrice,
            DiscountPercent = orderItem.DiscountPercent,
            DiscountAmount = orderItem.DiscountAmount,
            FinalUnitPrice = orderItem.FinalUnitPrice,
            PromotionNameSnapshot = orderItem.PromotionNameSnapshot,
            LensTypeId = orderItem.LensTypeId,
            LensPrice = orderItem.LensPrice,
            PrescriptionId = orderItem.PrescriptionId
        };
    }

    public async Task<OrderStatusUpdatedResponse> UpdateOrderStatusAsync(
        int staffUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nextOrderStatus = ParseOrderStatus(request.OrderStatus);
        var order = await _dbContext.Orders
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.Inventory)
            .Include(current => current.Payments)
                .ThenInclude(payment => payment.PaymentHistories)
            .Include(current => current.OrderStatusHistories)
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "ORDER_NOT_FOUND", "Order not found");
        }

        ValidateStatusTransition(order.OrderStatus, nextOrderStatus);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (nextOrderStatus == OrderStatus.Cancelled)
            {
                order.StaffId = staffUserId;
                await CancelOrderInternalAsync(
                    order,
                    staffUserId,
                    NormalizeText(request.Note) ?? "Order cancelled by staff.",
                    cancellationToken);
            }
            else
            {
                var now = DateTime.UtcNow;
                order.OrderStatus = nextOrderStatus;
                order.StaffId = staffUserId;
                order.UpdatedAt = now;

                if (nextOrderStatus == OrderStatus.Completed)
                {
                    CompletePaymentsForCompletedOrder(order, now);
                }

                order.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderStatus = nextOrderStatus,
                    UpdatedByUserId = staffUserId,
                    Note = NormalizeText(request.Note) ?? BuildDefaultStatusNote(nextOrderStatus),
                    UpdatedAt = now
                });

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new OrderStatusUpdatedResponse
        {
            Message = "Order status updated",
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(order.OrderStatus)
        };
    }

    public async Task<ShippingStatusUpdatedResponse> UpdateShippingStatusAsync(
        int staffUserId,
        int orderId,
        UpdateShippingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shippingStatus = ParseShippingStatus(request.ShippingStatus);
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "ORDER_NOT_FOUND", "Order not found");
        }

        order.ShippingStatus = shippingStatus;
        order.ShippingCode = NormalizeText(request.ShippingCode);
        order.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        order.StaffId = staffUserId;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ShippingStatusUpdatedResponse
        {
            Message = "Shipping status updated",
            ShippingStatus = ApiEnumMapper.ToApiShippingStatus(shippingStatus)
        };
    }

    public async Task<OrderStatusHistoriesResponse?> GetOrderStatusHistoriesAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetAccessibleOrderQuery(currentUserId, canAccessAllOrders, tracked: false)
            .Include(current => current.OrderStatusHistories)
            .AsSplitQuery()
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderStatusHistoriesResponse
        {
            Items = order.OrderStatusHistories
                .OrderBy(history => history.UpdatedAt)
                .ThenBy(history => history.HistoryId)
                .Select(history => new OrderStatusHistoryListItemResponse
                {
                    HistoryId = history.HistoryId,
                    OrderStatus = ApiEnumMapper.ToApiOrderStatus(history.OrderStatus),
                    Note = history.Note,
                    UpdatedAt = history.UpdatedAt
                })
                .ToList()
        };
    }

    private async Task<CreateOrderResult> CreateOrderAsync(
        int userId,
        OrderType orderType,
        string receiverName,
        string receiverPhone,
        string shippingAddress,
        PaymentMethod paymentMethod,
        IReadOnlyList<OrderCreationItem> items,
        int createdByUserId,
        bool requestPayOsInitialization,
        IReadOnlyCollection<CartItem>? cartItemsToRemove = null,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        var now = DateTime.UtcNow;
        var initialOrderStatus = orderType == OrderType.PreOrder
            ? OrderStatus.AwaitingStock
            : OrderStatus.Pending;
        var order = new Order
        {
            UserId = userId,
            OrderType = orderType,
            OrderStatus = initialOrderStatus,
            ReceiverName = receiverName,
            ReceiverPhone = receiverPhone,
            ShippingAddress = shippingAddress,
            ShippingStatus = ShippingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        var payment = new Payment
        {
            Amount = 0m,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            PaymentHistories =
            [
                new PaymentHistory
                {
                    PaymentStatus = PaymentStatus.Pending,
                    Notes = "Payment created.",
                    CreatedAt = now
                }
            ]
        };

        order.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderStatus = initialOrderStatus,
            UpdatedByUserId = createdByUserId,
            Note = "Order created.",
            UpdatedAt = now
        });

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            foreach (var item in items)
            {
                ValidateOrderVariant(item);

                if (item.RequirePreOrderEnabled)
                {
                    var inventory = item.Variant.Inventory;

                    if (inventory?.IsPreOrderAllowed != true || inventory.Quantity >= item.Quantity)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }
                }

                if (item.ReserveInventory)
                {
                    var reserved = await TryDeductInventoryAsync(item.Variant.VariantId, item.Quantity, cancellationToken);

                    if (!reserved)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }
                }

                order.TotalAmount += item.LineTotal;
                order.OrderItems.Add(new OrderItem
                {
                    VariantId = item.Variant.VariantId,
                    Quantity = item.Quantity,
                    SelectedColor = item.SelectedColor,
                    OriginalUnitPrice = item.OriginalUnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    FinalUnitPrice = item.FinalUnitPrice,
                    UnitPrice = item.UnitPrice,
                    PromotionNameSnapshot = item.PromotionNameSnapshot,
                    LensTypeId = item.LensTypeId,
                    LensPrice = item.LensPrice,
                    Prescription = item.Prescription
                });
            }

            payment.Amount = order.TotalAmount;
            order.Payments.Add(payment);

            await _unitOfWork.Repository<Order>().AddAsync(order);

            if (cartItemsToRemove is { Count: > 0 })
            {
                var detailRepository = _unitOfWork.Repository<CartPrescriptionDetail>();
                var cartItemRepository = _unitOfWork.Repository<CartItem>();
                var prescriptionDetails = cartItemsToRemove
                    .Where(item => item.CartPrescriptionDetail is not null)
                    .Select(item => item.CartPrescriptionDetail!)
                    .ToList();

                if (prescriptionDetails.Count > 0)
                {
                    detailRepository.RemoveRange(prescriptionDetails);
                }

                cartItemRepository.RemoveRange(cartItemsToRemove);

                var cart = cartItemsToRemove.First().Cart;
                cart.UpdatedAt = now;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        PaymentActionResponse? paymentAction = null;

        if (requestPayOsInitialization)
        {
            try
            {
                paymentAction = await _paymentService.InitializePayOsPaymentAsync(payment, order, null, cancellationToken);
            }
            catch (ApiException)
            {
                // Checkout/buy-now has already succeeded at this point.
                // The existing payment record remains reusable via /api/payments if FE needs to retry initialization.
            }
        }

        return new CreateOrderResult(order, payment, paymentAction);
    }

    private IReadOnlyList<OrderCreationItem> BuildCheckoutOrderItems(
        int userId,
        IReadOnlyCollection<CartItem> cartItems,
        OrderType orderType,
        DateTime now)
    {
        return cartItems
            .OrderBy(item => item.CartItemId)
            .Select(item =>
            {
                if (item.Variant is null || item.Variant.Product is null)
                {
                    throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                }

                var pricing = PromotionPricingHelper.Calculate(item.Variant, now);

                if (orderType == OrderType.Prescription)
                {
                    if (item.ItemType != CartItemType.PrescriptionConfigured)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }

                    if (item.Quantity != 1)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }

                    var detail = item.CartPrescriptionDetail;

                    if (detail is null || detail.LensType is null || !detail.LensType.IsActive)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }

                    if (item.Variant.Product.ProductType != ProductType.Frame || !item.Variant.Product.PrescriptionCompatible)
                    {
                        throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                    }

                    ValidatePrescriptionDetail(detail);
                    var calculatedPricing = _prescriptionPricingService.Calculate(
                        pricing.FinalPrice,
                        detail.LensType.Price,
                        detail.LensMaterial,
                        DeserializeCoatings(detail.Coatings),
                        item.Quantity,
                        errorCode: "CHECKOUT_FAILED",
                        errorMessage: "Unable to checkout selected items");
                    var serializedCoatings = SerializeCoatings(calculatedPricing.Coatings);

                    return new OrderCreationItem
                    {
                        Variant = item.Variant,
                        Quantity = item.Quantity,
                        SelectedColor = item.SelectedColor,
                        OriginalUnitPrice = pricing.OriginalPrice,
                        DiscountPercent = pricing.DiscountPercent,
                        DiscountAmount = pricing.DiscountAmount,
                        FinalUnitPrice = pricing.FinalPrice,
                        UnitPrice = pricing.FinalPrice,
                        PromotionNameSnapshot = pricing.PromotionName,
                        LineTotal = calculatedPricing.TotalPrice,
                        ReserveInventory = true,
                        LensTypeId = detail.LensTypeId,
                        LensPrice = calculatedPricing.LensPrice,
                        Prescription = new PrescriptionSpec
                        {
                            UserId = userId,
                            LensTypeId = detail.LensTypeId,
                            LensTypeCode = detail.LensType.LensCode,
                            LensMaterial = calculatedPricing.LensMaterial,
                            Coatings = serializedCoatings,
                            LensBasePrice = calculatedPricing.LensBasePrice,
                            MaterialPrice = calculatedPricing.MaterialPrice,
                            CoatingPrice = calculatedPricing.CoatingPrice,
                            TotalLensPrice = calculatedPricing.LensPrice,
                            SphLeft = detail.SphLeft,
                            SphRight = detail.SphRight,
                            CylLeft = detail.CylLeft,
                            CylRight = detail.CylRight,
                            AxisLeft = detail.AxisLeft,
                            AxisRight = detail.AxisRight,
                            Pd = detail.Pd,
                            PrescriptionImage = NormalizePrescriptionImageReference(detail.PrescriptionImage),
                            PrescriptionStatus = PrescriptionStatus.Submitted,
                            Notes = NormalizeOptionalNote(detail.Notes),
                            CreatedAt = now
                        }
                    };
                }

                if (item.ItemType != CartItemType.Standard)
                {
                    throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
                }

                return new OrderCreationItem
                {
                    Variant = item.Variant,
                    Quantity = item.Quantity,
                    SelectedColor = item.SelectedColor,
                    OriginalUnitPrice = pricing.OriginalPrice,
                    DiscountPercent = pricing.DiscountPercent,
                    DiscountAmount = pricing.DiscountAmount,
                    FinalUnitPrice = pricing.FinalPrice,
                    UnitPrice = pricing.FinalPrice,
                    PromotionNameSnapshot = pricing.PromotionName,
                    LineTotal = pricing.FinalPrice * item.Quantity,
                    ReserveInventory = orderType == OrderType.Ready,
                    RequirePreOrderEnabled = orderType == OrderType.PreOrder
                };
            })
            .ToList();
    }

    private static void ValidatePrescriptionDetail(CartPrescriptionDetail detail)
    {
        ValidateAxis(detail.AxisLeft);
        ValidateAxis(detail.AxisRight);

        if (detail.Pd <= 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }
    }

    private static void ValidateAxis(int axis)
    {
        if (axis is < 0 or > 180)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }
    }

    private static IReadOnlyList<string> DeserializeCoatings(string? serializedCoatings)
    {
        if (string.IsNullOrWhiteSpace(serializedCoatings))
        {
            return [];
        }

        return serializedCoatings
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? SerializeCoatings(IReadOnlyCollection<string>? coatings)
    {
        if (coatings is null || coatings.Count == 0)
        {
            return null;
        }

        var serialized = string.Join(",", coatings);

        if (serialized.Length > 500)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        return serialized;
    }

    private static string? NormalizePrescriptionImageReference(string? imageReference)
    {
        var normalized = NormalizeText(imageReference);

        if (normalized is not null
            && normalized.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        if (normalized is not null && normalized.Length > 500)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        return normalized;
    }

    private static string? NormalizeOptionalNote(string? note)
    {
        var normalized = NormalizeText(note);

        if (normalized is not null && normalized.Length > 255)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        return normalized;
    }

    private static IReadOnlyList<int> PrepareCartItemIds(IReadOnlyCollection<int>? cartItemIds)
    {
        if (cartItemIds is null || cartItemIds.Count == 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        var preparedIds = cartItemIds
            .Where(item => item > 0)
            .Distinct()
            .ToList();

        if (preparedIds.Count != cartItemIds.Count || preparedIds.Count == 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        return preparedIds;
    }

    private static OrderType ResolveCheckoutOrderType(IEnumerable<CartItem> cartItems)
    {
        var distinctOrderTypes = cartItems
            .Select(item => item.OrderType)
            .Distinct()
            .ToList();

        if (distinctOrderTypes.Count != 1)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        return distinctOrderTypes[0];
    }

    private async Task CancelOrderInternalAsync(
        Order order,
        int updatedByUserId,
        string note,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var orderItem in order.OrderItems)
        {
            if (order.OrderType != OrderType.PreOrder)
            {
                var inventory = orderItem.Variant.Inventory;

                if (inventory is null)
                {
                    inventory = new Inventory
                    {
                        VariantId = orderItem.VariantId,
                        Quantity = 0,
                        IsPreOrderAllowed = false
                    };

                    await _unitOfWork.Repository<Inventory>().AddAsync(inventory);
                    orderItem.Variant.Inventory = inventory;
                }

                inventory.Quantity += orderItem.Quantity;
            }
        }

        order.OrderStatus = OrderStatus.Cancelled;
        order.ShippingStatus = null;
        order.UpdatedAt = now;
        order.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderStatus = OrderStatus.Cancelled,
            UpdatedByUserId = updatedByUserId,
            Note = note,
            UpdatedAt = now
        });

        foreach (var payment in order.Payments.Where(payment => payment.PaymentStatus != PaymentStatus.Completed))
        {
            payment.PaymentStatus = PaymentStatus.Failed;
            payment.PaymentHistories.Add(new PaymentHistory
            {
                PaymentStatus = PaymentStatus.Failed,
                Notes = "Payment closed because the order was cancelled.",
                CreatedAt = now
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryDeductInventoryAsync(int variantId, int requestedQuantity, CancellationToken cancellationToken)
    {
        var affectedRows = await _dbContext.Inventory
            .Where(inventory => inventory.VariantId == variantId && inventory.Quantity >= requestedQuantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    inventory => inventory.Quantity,
                    inventory => inventory.Quantity - requestedQuantity),
                cancellationToken);

        return affectedRows == 1;
    }

    private IQueryable<Order> GetAccessibleOrderQuery(int currentUserId, bool canAccessAllOrders, bool tracked)
    {
        var query = tracked ? _dbContext.Orders : _dbContext.Orders.AsNoTracking();

        if (!canAccessAllOrders)
        {
            query = query.Where(order => order.UserId == currentUserId);
        }

        return query;
    }

    private static void ValidateOrderVariant(OrderCreationItem item)
    {
        if (!item.Variant.IsActive || !item.Variant.Product.IsActive)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }

        if ((item.ReserveInventory || item.RequirePreOrderEnabled) && item.Variant.Inventory is null)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "CHECKOUT_FAILED", "Unable to checkout selected items");
        }
    }

    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_STATUS", "Invalid order status update");
        }

        if (!AllowedStatusTransitions.TryGetValue(currentStatus, out var allowedStatuses)
            || !allowedStatuses.Contains(nextStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_STATUS", "Invalid order status update");
        }
    }

    private static void CompletePaymentsForCompletedOrder(Order order, DateTime completedAt)
    {
        var activePayments = order.Payments
            .Where(payment => payment.PaymentStatus != PaymentStatus.Failed)
            .ToList();

        if (activePayments.Count == 0)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_STATUS", "Invalid order status update");
        }

        if (activePayments.Any(payment => payment.PaymentMethod != PaymentMethod.COD && payment.PaymentStatus != PaymentStatus.Completed))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_STATUS", "Invalid order status update");
        }

        foreach (var payment in activePayments.Where(payment =>
                     payment.PaymentMethod == PaymentMethod.COD &&
                     payment.PaymentStatus == PaymentStatus.Pending))
        {
            payment.PaymentStatus = PaymentStatus.Completed;
            payment.PaidAt = completedAt;
            payment.PaymentHistories.Add(new PaymentHistory
            {
                PaymentStatus = PaymentStatus.Completed,
                Notes = "Payment collected when the order was completed.",
                CreatedAt = completedAt
            });
        }
    }

    private static IOrderedQueryable<Order> ApplyOrderSorting(IQueryable<Order> query, string? sortBy, bool descending)
    {
        var normalizedSortBy = NormalizeSortField(sortBy);

        return normalizedSortBy switch
        {
            null or "createdat" => descending
                ? query.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.OrderId)
                : query.OrderBy(order => order.CreatedAt).ThenBy(order => order.OrderId),
            "updatedat" => descending
                ? query.OrderByDescending(order => order.UpdatedAt).ThenByDescending(order => order.OrderId)
                : query.OrderBy(order => order.UpdatedAt).ThenBy(order => order.OrderId),
            "totalamount" => descending
                ? query.OrderByDescending(order => order.TotalAmount).ThenByDescending(order => order.OrderId)
                : query.OrderBy(order => order.TotalAmount).ThenBy(order => order.OrderId),
            "orderid" => descending
                ? query.OrderByDescending(order => order.OrderId)
                : query.OrderBy(order => order.OrderId),
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid order query")
        };
    }

    private static string BuildDefaultStatusNote(OrderStatus orderStatus)
    {
        return orderStatus switch
        {
            OrderStatus.Confirmed => "Order confirmed.",
            OrderStatus.AwaitingStock => "Order moved to awaiting stock.",
            OrderStatus.Processing => "Order is being processed.",
            OrderStatus.Shipped => "Order shipped.",
            OrderStatus.Completed => "Order completed.",
            OrderStatus.Cancelled => "Order cancelled.",
            _ => $"Order status updated to {ApiEnumMapper.ToApiOrderStatus(orderStatus)}."
        };
    }

    private static PaymentMethod ParsePaymentMethod(string? value)
    {
        if (!ApiEnumMapper.TryParsePaymentMethod(value, out var paymentMethod))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        if (paymentMethod != PaymentMethod.COD && paymentMethod != PaymentMethod.PayOS)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        return paymentMethod;
    }

    private static OrderType ParseOrderType(string? value)
    {
        if (!ApiEnumMapper.TryParseOrderType(value, out var orderType))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid order query");
        }

        return orderType;
    }

    private static OrderStatus ParseOrderStatus(string? value)
    {
        if (!ApiEnumMapper.TryParseOrderStatus(value, out var orderStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_STATUS", "Invalid order status update");
        }

        return orderStatus;
    }

    private static ShippingStatus ParseShippingStatus(string? value)
    {
        if (!ApiEnumMapper.TryParseShippingStatus(value, out var shippingStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_SHIPPING_STATUS", "Invalid shipping status update");
        }

        return shippingStatus;
    }

    private static PaymentStatus ParsePaymentStatus(string? value)
    {
        if (!ApiEnumMapper.TryParsePaymentStatus(value, out var paymentStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid order query");
        }

        return paymentStatus;
    }

    private static bool ParseSortOrder(string? sortOrder)
    {
        var normalizedSortOrder = NormalizeSortField(sortOrder);

        return normalizedSortOrder switch
        {
            null or "desc" => true,
            "asc" => false,
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid order query")
        };
    }

    private static string NormalizeRequiredText(string? value, string field)
    {
        var normalized = NormalizeText(value);

        if (normalized is null)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_ORDER_REQUEST", $"{field} is required");
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static string? NormalizeSortField(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Trim()
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
    }

    private static CheckoutPaymentResponse MapCheckoutPayment(Payment payment, PaymentActionResponse? paymentAction)
    {
        return new CheckoutPaymentResponse
        {
            PaymentId = payment.PaymentId,
            PaymentStatus = paymentAction?.PaymentStatus ?? ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus),
            PayUrl = paymentAction?.PayUrl,
            Deeplink = paymentAction?.Deeplink,
            QrCodeUrl = paymentAction?.QrCodeUrl
        };
    }

    private static OrderSummaryResponse MapOrderSummary(Order order)
    {
        var latestPayment = order.Payments
            .OrderByDescending(payment => payment.PaymentId)
            .FirstOrDefault();

        return new OrderSummaryResponse
        {
            OrderId = order.OrderId,
            OrderType = ApiEnumMapper.ToApiOrderType(order.OrderType),
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(order.OrderStatus),
            ShippingStatus = order.ShippingStatus is null ? null : ApiEnumMapper.ToApiShippingStatus(order.ShippingStatus.Value),
            TotalAmount = order.TotalAmount,
            ItemCount = order.OrderItems.Sum(item => item.Quantity),
            ReceiverName = order.ReceiverName,
            PaymentMethod = latestPayment is null ? null : ApiEnumMapper.ToApiPaymentMethod(latestPayment.PaymentMethod),
            PaymentStatus = latestPayment is null ? null : ApiEnumMapper.ToApiPaymentStatus(latestPayment.PaymentStatus),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    private static OrderDetailResponse MapOrderDetail(Order order)
    {
        var latestPayment = order.Payments
            .OrderByDescending(payment => payment.PaymentId)
            .FirstOrDefault();

        return new OrderDetailResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            OrderType = ApiEnumMapper.ToApiOrderType(order.OrderType),
            OrderStatus = ApiEnumMapper.ToApiOrderStatus(order.OrderStatus),
            TotalAmount = order.TotalAmount,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            ShippingAddress = order.ShippingAddress,
            ShippingCode = order.ShippingCode,
            ShippingStatus = order.ShippingStatus is null ? null : ApiEnumMapper.ToApiShippingStatus(order.ShippingStatus.Value),
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            StaffId = order.StaffId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderItemResponse
                {
                    OrderItemId = item.OrderItemId,
                    VariantId = item.VariantId,
                    ProductId = item.Variant.ProductId,
                    ProductName = item.Variant.Product.ProductName,
                    Sku = item.Variant.Sku,
                    VariantColor = item.Variant.Color,
                    SelectedColor = item.SelectedColor,
                    Quantity = item.Quantity,
                    OriginalUnitPrice = item.OriginalUnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    FinalUnitPrice = item.FinalUnitPrice,
                    UnitPrice = item.UnitPrice,
                    PromotionNameSnapshot = item.PromotionNameSnapshot,
                    LineTotal = (item.UnitPrice + (item.LensPrice ?? 0m)) * item.Quantity,
                    LensTypeId = item.LensTypeId,
                    LensPrice = item.LensPrice,
                    PrescriptionId = item.PrescriptionId
                })
                .ToList(),
            Payment = latestPayment is null
                ? null
                : new OrderPaymentResponse
                {
                    PaymentId = latestPayment.PaymentId,
                    Amount = latestPayment.Amount,
                    PaymentMethod = ApiEnumMapper.ToApiPaymentMethod(latestPayment.PaymentMethod),
                    PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(latestPayment.PaymentStatus),
                    PaidAt = latestPayment.PaidAt,
                    Histories = latestPayment.PaymentHistories
                        .OrderBy(history => history.CreatedAt)
                        .ThenBy(history => history.PaymentHistoryId)
                        .Select(history => new OrderPaymentHistoryResponse
                        {
                            PaymentHistoryId = history.PaymentHistoryId,
                            PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(history.PaymentStatus),
                            TransactionCode = history.TransactionCode,
                            Notes = history.Notes,
                            CreatedAt = history.CreatedAt
                        })
                        .ToList()
                },
            StatusHistory = order.OrderStatusHistories
                .OrderBy(history => history.UpdatedAt)
                .ThenBy(history => history.HistoryId)
                .Select(history => new OrderStatusHistoryResponse
                {
                    HistoryId = history.HistoryId,
                    OrderStatus = ApiEnumMapper.ToApiOrderStatus(history.OrderStatus),
                    UpdatedByUserId = history.UpdatedByUserId,
                    UpdatedByName = history.UpdatedByUser?.FullName,
                    Note = history.Note,
                    UpdatedAt = history.UpdatedAt
                })
                .ToList()
        };
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string errorCode, string message, object? details = null)
    {
        return new ApiException((int)statusCode, errorCode, message, details);
    }

    private sealed class OrderCreationItem
    {
        public required ProductVariant Variant { get; init; }

        public int Quantity { get; init; }

        public string? SelectedColor { get; init; }

        public decimal OriginalUnitPrice { get; init; }

        public decimal DiscountPercent { get; init; }

        public decimal DiscountAmount { get; init; }

        public decimal FinalUnitPrice { get; init; }

        public decimal UnitPrice { get; init; }

        public string? PromotionNameSnapshot { get; init; }

        public decimal LineTotal { get; init; }

        public bool ReserveInventory { get; init; }

        public bool RequirePreOrderEnabled { get; init; }

        public int? LensTypeId { get; init; }

        public decimal? LensPrice { get; init; }

        public PrescriptionSpec? Prescription { get; init; }
    }

    private sealed record CreateOrderResult(
        Order Order,
        Payment Payment,
        PaymentActionResponse? PaymentAction);
}
