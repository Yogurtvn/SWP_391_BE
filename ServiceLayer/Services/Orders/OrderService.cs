using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Data;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Orders;
using ServiceLayer.DTOs.Orders;

namespace ServiceLayer.Services.Orders;

public class OrderService(IUnitOfWork unitOfWork, OnlineEyewearDbContext dbContext) : IOrderService
{
    private static readonly HashSet<OrderStatus> CancellableStatuses =
    [
        OrderStatus.Pending,
        OrderStatus.Confirmed,
        OrderStatus.AwaitingStock
    ];

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

    public async Task<OrderDetailResponse> CheckoutReadyOrderAsync(
        int userId,
        ReadyOrderCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var receiverName = NormalizeRequiredText(request.ReceiverName, "Receiver name");
        var receiverPhone = NormalizeRequiredText(request.ReceiverPhone, "Receiver phone");
        var shippingAddress = NormalizeRequiredText(request.ShippingAddress, "Shipping address");

        if (!TryParsePaymentMethod(request.PaymentMethod, out var paymentMethod))
        {
            throw new InvalidOperationException("Payment method is invalid. Use COD, VNPay, Momo, or their numeric values.");
        }

        var requestedItems = PrepareCheckoutItems(request.Items);
        var userRepository = _unitOfWork.Repository<User>();
        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var orderRepository = _unitOfWork.Repository<Order>();

        var userExists = await userRepository.ExistsAsync(user => user.UserId == userId && user.IsActive);

        if (!userExists)
        {
            throw new KeyNotFoundException("Authenticated user was not found or is inactive.");
        }

        var checkoutItems = requestedItems
            .OrderBy(item => item.VariantId)
            .ToList();
        var variantIds = checkoutItems.Select(item => item.VariantId).ToHashSet();
        var now = DateTime.UtcNow;
        var totalAmount = 0m;
        var order = new Order
        {
            UserId = userId,
            OrderType = OrderType.Ready,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = 0m,
            ReceiverName = receiverName,
            ReceiverPhone = receiverPhone,
            ShippingAddress = shippingAddress,
            ShippingStatus = ShippingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var variants = (await variantRepository.FindAsync(
                variant => variantIds.Contains(variant.VariantId),
                includeProperties: "Product,Inventory",
                tracked: true))
                .ToList();

            if (variants.Count != variantIds.Count)
            {
                var foundVariantIds = variants.Select(variant => variant.VariantId).ToHashSet();
                var missingVariantIds = variantIds.Where(variantId => !foundVariantIds.Contains(variantId));
                throw new KeyNotFoundException($"Variant not found: {string.Join(", ", missingVariantIds)}.");
            }

            var variantsById = variants.ToDictionary(variant => variant.VariantId);

            foreach (var requestedItem in checkoutItems)
            {
                var variant = variantsById[requestedItem.VariantId];

                ValidateVariantForReadyOrder(variant);

                var inventoryDeducted = await TryDeductInventoryAsync(
                    variant.VariantId,
                    requestedItem.Quantity,
                    cancellationToken);

                if (!inventoryDeducted)
                {
                    var availableQuantity = await GetAvailableInventoryQuantityAsync(variant.VariantId, cancellationToken);
                    throw new InvalidOperationException(
                        $"Variant {variant.VariantId} only has {availableQuantity} item(s) in stock.");
                }

                var lineTotal = variant.Price * requestedItem.Quantity;
                totalAmount += lineTotal;

                order.OrderItems.Add(new OrderItem
                {
                    VariantId = variant.VariantId,
                    Variant = variant,
                    Quantity = requestedItem.Quantity,
                    SelectedColor = requestedItem.SelectedColor ?? variant.Color,
                    UnitPrice = variant.Price
                });
            }

            var payment = new Payment
            {
                Amount = totalAmount,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                PaymentHistories =
                [
                    new PaymentHistory
                    {
                        PaymentStatus = PaymentStatus.Pending,
                        Notes = "Order checkout created.",
                        CreatedAt = now
                    }
                ]
            };

            order.TotalAmount = totalAmount;
            order.Payments.Add(payment);
            order.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderStatus = OrderStatus.Pending,
                UpdatedByUserId = userId,
                Note = "Order created by customer.",
                UpdatedAt = now
            });

            await orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return MapOrderDetail(order);
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> GetMyOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>();
        var orders = await orderRepository.FindAsync(
            filter: order => order.UserId == userId,
            orderBy: query => query.OrderByDescending(order => order.CreatedAt),
            includeProperties: "OrderItems,Payments",
            tracked: false);

        return orders
            .Select(MapOrderSummary)
            .ToList();
    }

    public async Task<OrderDetailResponse?> GetMyOrderByIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderForUserAsync(userId, orderId, tracked: false);
        return order is null ? null : MapOrderDetail(order);
    }

    public async Task<OrderDetailResponse?> UpdateOrderStatusAsync(
        int staffUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseOrderStatus(request.OrderStatus, out var nextOrderStatus))
        {
            throw new InvalidOperationException("Order status is invalid.");
        }

        var providedShippingStatus = NormalizeOptionalText(request.ShippingStatus);
        ShippingStatus? nextShippingStatus = null;

        if (providedShippingStatus is not null)
        {
            if (!TryParseShippingStatus(providedShippingStatus, out var parsedShippingStatus))
            {
                throw new InvalidOperationException("Shipping status is invalid.");
            }

            nextShippingStatus = parsedShippingStatus;
        }

        var note = NormalizeOptionalText(request.Note);
        var orderRepository = _unitOfWork.Repository<Order>();
        var order = await orderRepository.GetFirstOrDefaultAsync(
            currentOrder => currentOrder.OrderId == orderId && currentOrder.OrderType == OrderType.Ready,
            includeProperties: "OrderItems.Variant.Product,OrderItems.Variant.Inventory,OrderStatusHistories.UpdatedByUser,Payments.PaymentHistories",
            tracked: true);

        if (order is null)
        {
            return null;
        }

        ValidateStatusTransition(order.OrderStatus, nextOrderStatus);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (nextOrderStatus == OrderStatus.Cancelled)
            {
                order.StaffId = staffUserId;
                await CancelOrderAsync(
                    order,
                    staffUserId,
                    note ?? "Order cancelled by staff.",
                    cancellationToken);
            }
            else
            {
                var now = DateTime.UtcNow;

                order.OrderStatus = nextOrderStatus;
                order.ShippingStatus = ResolveShippingStatus(nextOrderStatus, nextShippingStatus);
                order.StaffId = staffUserId;
                order.UpdatedAt = now;

                if (nextOrderStatus == OrderStatus.Completed)
                {
                    CompletePaymentsForDeliveredOrder(order, now);
                }

                order.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderStatus = nextOrderStatus,
                    UpdatedByUserId = staffUserId,
                    Note = note ?? BuildDefaultStatusNote(nextOrderStatus),
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

        return MapOrderDetail(order);
    }

    public async Task<CancelOrderResult> CancelMyOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderForUserAsync(
            userId,
            orderId,
            tracked: true,
            includeProperties: "OrderItems.Variant.Product,OrderItems.Variant.Inventory,OrderStatusHistories.UpdatedByUser,Payments.PaymentHistories");

        if (order is null)
        {
            return new CancelOrderResult
            {
                ErrorCode = "ORDER_NOT_FOUND",
                Message = "Order not found."
            };
        }

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            return new CancelOrderResult
            {
                ErrorCode = "ORDER_ALREADY_CANCELLED",
                Message = "Order has already been cancelled."
            };
        }

        if (!CancellableStatuses.Contains(order.OrderStatus))
        {
            return new CancelOrderResult
            {
                ErrorCode = "ORDER_CANNOT_BE_CANCELLED",
                Message = "Only pending or confirmed orders can be cancelled."
            };
        }

        if (order.Payments.Any(payment => payment.PaymentStatus == PaymentStatus.Completed))
        {
            return new CancelOrderResult
            {
                ErrorCode = "PAYMENT_ALREADY_COMPLETED",
                Message = "Order cannot be cancelled because payment has already been completed."
            };
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await CancelOrderAsync(order, userId, "Order cancelled by customer.", cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new CancelOrderResult
        {
            Succeeded = true,
            Message = "Order cancelled successfully.",
            Order = MapOrderDetail(order)
        };
    }

    private async Task<Order?> GetOrderForUserAsync(
        int userId,
        int orderId,
        bool tracked,
        string includeProperties = "OrderItems.Variant.Product,OrderStatusHistories.UpdatedByUser,Payments.PaymentHistories")
    {
        var orderRepository = _unitOfWork.Repository<Order>();
        return await orderRepository.GetFirstOrDefaultAsync(
            order => order.OrderId == orderId && order.UserId == userId,
            includeProperties: includeProperties,
            tracked: tracked);
    }

    private async Task CancelOrderAsync(
        Order order,
        int updatedByUserId,
        string note,
        CancellationToken cancellationToken)
    {
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var now = DateTime.UtcNow;

        foreach (var orderItem in order.OrderItems)
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

                await inventoryRepository.AddAsync(inventory);
                orderItem.Variant.Inventory = inventory;
            }

            inventory.Quantity += orderItem.Quantity;
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

    private static List<PreparedCheckoutItem> PrepareCheckoutItems(IEnumerable<ReadyOrderCheckoutItemRequest> items)
    {
        var preparedItems = items
            .Select(item => new PreparedCheckoutItem(
                item.VariantId,
                item.Quantity,
                NormalizeOptionalText(item.SelectedColor)))
            .ToList();

        if (preparedItems.Count == 0)
        {
            throw new InvalidOperationException("At least one order item is required.");
        }

        var invalidItem = preparedItems.FirstOrDefault(item => item.Quantity <= 0 || item.VariantId <= 0);

        if (invalidItem is not null)
        {
            throw new InvalidOperationException("Each order item must have a valid variant id and quantity greater than zero.");
        }

        var duplicateVariantId = preparedItems
            .GroupBy(item => item.VariantId)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateVariantId is not null)
        {
            throw new InvalidOperationException($"Variant {duplicateVariantId.Value} appears more than once in the checkout request.");
        }

        return preparedItems;
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

    private async Task<int> GetAvailableInventoryQuantityAsync(int variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Inventory
            .Where(inventory => inventory.VariantId == variantId)
            .Select(inventory => (int?)inventory.Quantity)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;
    }

    private static void ValidateVariantForReadyOrder(ProductVariant variant)
    {
        if (!variant.IsActive || !variant.Product.IsActive)
        {
            throw new InvalidOperationException($"Variant {variant.VariantId} is not available for ordering.");
        }

        if (variant.Inventory is null)
        {
            throw new InvalidOperationException($"Variant {variant.VariantId} does not have inventory configured.");
        }
    }

    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            throw new InvalidOperationException("The order is already in the requested status.");
        }

        if (!AllowedStatusTransitions.TryGetValue(currentStatus, out var allowedStatuses)
            || !allowedStatuses.Contains(nextStatus))
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {currentStatus} to {nextStatus}.");
        }
    }

    private static ShippingStatus? ResolveShippingStatus(
        OrderStatus orderStatus,
        ShippingStatus? requestedShippingStatus)
    {
        ShippingStatus? defaultShippingStatus = orderStatus switch
        {
            OrderStatus.Pending => ShippingStatus.Pending,
            OrderStatus.Confirmed => ShippingStatus.Pending,
            OrderStatus.AwaitingStock => ShippingStatus.Pending,
            OrderStatus.Processing => ShippingStatus.Picking,
            OrderStatus.Shipped => ShippingStatus.Delivering,
            OrderStatus.Completed => ShippingStatus.Delivered,
            OrderStatus.Cancelled => (ShippingStatus?)null,
            _ => (ShippingStatus?)null
        };

        if (requestedShippingStatus is null)
        {
            return defaultShippingStatus;
        }

        var isValidCombination = orderStatus switch
        {
            OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.AwaitingStock
                => requestedShippingStatus == ShippingStatus.Pending,
            OrderStatus.Processing
                => requestedShippingStatus == ShippingStatus.Picking,
            OrderStatus.Shipped
                => requestedShippingStatus == ShippingStatus.Delivering,
            OrderStatus.Completed
                => requestedShippingStatus == ShippingStatus.Delivered,
            OrderStatus.Cancelled
                => false,
            _ => false
        };

        if (!isValidCombination)
        {
            throw new InvalidOperationException(
                $"Shipping status {requestedShippingStatus} is not valid for order status {orderStatus}.");
        }

        return requestedShippingStatus;
    }

    private static void CompletePaymentsForDeliveredOrder(Order order, DateTime completedAt)
    {
        var activePayments = order.Payments
            .Where(payment => payment.PaymentStatus != PaymentStatus.Failed)
            .ToList();

        if (activePayments.Count == 0)
        {
            throw new InvalidOperationException("The order does not have an active payment to complete.");
        }

        var hasUnpaidOnlinePayment = activePayments.Any(payment =>
            payment.PaymentMethod != PaymentMethod.COD &&
            payment.PaymentStatus != PaymentStatus.Completed);

        if (hasUnpaidOnlinePayment)
        {
            throw new InvalidOperationException(
                "Online payment must be completed before the order can be marked as completed.");
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

    private static string BuildDefaultStatusNote(OrderStatus orderStatus)
    {
        return orderStatus switch
        {
            OrderStatus.Confirmed => "Order confirmed by staff.",
            OrderStatus.AwaitingStock => "Order moved to awaiting stock.",
            OrderStatus.Processing => "Order is being packed.",
            OrderStatus.Shipped => "Order handed over for delivery.",
            OrderStatus.Completed => "Order completed successfully.",
            OrderStatus.Cancelled => "Order cancelled.",
            _ => $"Order status updated to {orderStatus}."
        };
    }

    private static bool TryParsePaymentMethod(string? input, out PaymentMethod paymentMethod)
    {
        paymentMethod = default;
        var normalizedInput = NormalizeOptionalText(input);

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        if (Enum.TryParse<PaymentMethod>(normalizedInput, ignoreCase: true, out paymentMethod)
            && Enum.IsDefined(paymentMethod))
        {
            return true;
        }

        if (byte.TryParse(normalizedInput, out var numericValue)
            && Enum.IsDefined(typeof(PaymentMethod), numericValue))
        {
            paymentMethod = (PaymentMethod)numericValue;
            return true;
        }

        return false;
    }

    private static bool TryParseOrderStatus(string? input, out OrderStatus orderStatus)
    {
        orderStatus = default;
        var normalizedInput = NormalizeOptionalText(input);

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        if (Enum.TryParse<OrderStatus>(normalizedInput, ignoreCase: true, out orderStatus)
            && Enum.IsDefined(orderStatus))
        {
            return true;
        }

        if (byte.TryParse(normalizedInput, out var numericValue)
            && Enum.IsDefined(typeof(OrderStatus), numericValue))
        {
            orderStatus = (OrderStatus)numericValue;
            return true;
        }

        return false;
    }

    private static bool TryParseShippingStatus(string? input, out ShippingStatus shippingStatus)
    {
        shippingStatus = default;
        var normalizedInput = NormalizeOptionalText(input);

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        if (Enum.TryParse<ShippingStatus>(normalizedInput, ignoreCase: true, out shippingStatus)
            && Enum.IsDefined(shippingStatus))
        {
            return true;
        }

        if (byte.TryParse(normalizedInput, out var numericValue)
            && Enum.IsDefined(typeof(ShippingStatus), numericValue))
        {
            shippingStatus = (ShippingStatus)numericValue;
            return true;
        }

        return false;
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalizedValue = NormalizeOptionalText(value);

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static OrderSummaryResponse MapOrderSummary(Order order)
    {
        var latestPayment = order.Payments
            .OrderByDescending(payment => payment.PaymentId)
            .FirstOrDefault();

        return new OrderSummaryResponse
        {
            OrderId = order.OrderId,
            OrderType = order.OrderType.ToString(),
            OrderStatus = order.OrderStatus.ToString(),
            ShippingStatus = order.ShippingStatus?.ToString(),
            TotalAmount = order.TotalAmount,
            ItemCount = order.OrderItems.Sum(item => item.Quantity),
            ReceiverName = order.ReceiverName,
            PaymentMethod = latestPayment?.PaymentMethod.ToString(),
            PaymentStatus = latestPayment?.PaymentStatus.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    private static OrderDetailResponse MapOrderDetail(Order order)
    {
        return new OrderDetailResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            OrderType = order.OrderType.ToString(),
            OrderStatus = order.OrderStatus.ToString(),
            TotalAmount = order.TotalAmount,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            ShippingAddress = order.ShippingAddress,
            ShippingCode = order.ShippingCode,
            ShippingStatus = order.ShippingStatus?.ToString(),
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
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToList(),
            Payments = order.Payments
                .OrderBy(payment => payment.PaymentId)
                .Select(payment => new OrderPaymentResponse
                {
                    PaymentId = payment.PaymentId,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod.ToString(),
                    PaymentStatus = payment.PaymentStatus.ToString(),
                    PaidAt = payment.PaidAt,
                    Histories = payment.PaymentHistories
                        .OrderBy(history => history.CreatedAt)
                        .ThenBy(history => history.PaymentHistoryId)
                        .Select(history => new OrderPaymentHistoryResponse
                        {
                            PaymentHistoryId = history.PaymentHistoryId,
                            PaymentStatus = history.PaymentStatus.ToString(),
                            TransactionCode = history.TransactionCode,
                            Notes = history.Notes,
                            CreatedAt = history.CreatedAt
                        })
                        .ToList()
                })
                .ToList(),
            StatusHistories = order.OrderStatusHistories
                .OrderBy(history => history.UpdatedAt)
                .ThenBy(history => history.HistoryId)
                .Select(history => new OrderStatusHistoryResponse
                {
                    HistoryId = history.HistoryId,
                    OrderStatus = history.OrderStatus.ToString(),
                    UpdatedByUserId = history.UpdatedByUserId,
                    UpdatedByName = history.UpdatedByUser?.FullName,
                    Note = history.Note,
                    UpdatedAt = history.UpdatedAt
                })
                .ToList()
        };
    }

    private sealed record PreparedCheckoutItem(int VariantId, int Quantity, string? SelectedColor);
}
