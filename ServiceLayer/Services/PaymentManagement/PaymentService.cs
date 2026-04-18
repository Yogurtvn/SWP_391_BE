using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace ServiceLayer.Services.PaymentManagement;

public class PaymentService(
    IUnitOfWork unitOfWork,
    OnlineEyewearDbContext dbContext,
    IPayOsGatewayClient payOsGatewayClient) : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;
    private readonly IPayOsGatewayClient _payOsGatewayClient = payOsGatewayClient;

    public async Task<CreatePaymentResponse> CreatePaymentAsync(
        int currentUserId,
        bool canAccessAllOrders,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderId = request.OrderId.GetValueOrDefault();
        var requestedAmount = request.Amount;

        if (orderId <= 0
            || requestedAmount is null
            || requestedAmount < 0m
            || !ApiEnumMapper.TryParsePaymentMethod(request.PaymentMethod, out var paymentMethod)
            || !IsSupportedPaymentMethod(paymentMethod))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        var order = await _dbContext.Orders
            .Include(current => current.Payments)
                .ThenInclude(payment => payment.PaymentHistories)
            .FirstOrDefaultAsync(
                current => current.OrderId == orderId && (canAccessAllOrders || current.UserId == currentUserId),
                cancellationToken);

        if (order is null || requestedAmount.Value != order.TotalAmount)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        Payment payment;

        if (order.Payments.Count > 0)
        {
            payment = order.Payments
                .OrderByDescending(current => current.PaymentId)
                .First();

            if (payment.PaymentStatus == PaymentStatus.Completed
                || payment.Amount != requestedAmount.Value
                || payment.PaymentMethod != paymentMethod)
            {
                throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
            }
        }
        else
        {
            var now = DateTime.UtcNow;
            payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = requestedAmount.Value,
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

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _unitOfWork.Repository<Payment>().AddAsync(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        var paymentAction = payment.PaymentMethod == PaymentMethod.PayOS
            ? await InitializePayOsPaymentAsync(payment, order, null, cancellationToken)
            : null;

        return new CreatePaymentResponse
        {
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            PaymentMethod = ApiEnumMapper.ToApiPaymentMethod(payment.PaymentMethod),
            PaymentStatus = paymentAction?.PaymentStatus ?? ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus),
            PayUrl = paymentAction?.PayUrl,
            Deeplink = paymentAction?.Deeplink,
            QrCodeUrl = paymentAction?.QrCodeUrl
        };
    }

    public async Task<PagedResult<PaymentListItemResponse>> GetPaymentsAsync(
        GetPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.Payments
            .AsNoTracking()
            .Include(payment => payment.PaymentHistories)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            if (!ApiEnumMapper.TryParsePaymentMethod(request.PaymentMethod, out var paymentMethod))
            {
                throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid payment query");
            }

            query = query.Where(payment => payment.PaymentMethod == paymentMethod);
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            if (!ApiEnumMapper.TryParsePaymentStatus(request.PaymentStatus, out var paymentStatus))
            {
                throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid payment query");
            }

            query = query.Where(payment => payment.PaymentStatus == paymentStatus);
        }

        if (request.OrderId.HasValue)
        {
            query = query.Where(payment => payment.OrderId == request.OrderId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;
            query = query.Where(payment =>
                payment.PaymentHistories.Min(history => (DateTime?)history.CreatedAt) >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDateExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(payment =>
                payment.PaymentHistories.Min(history => (DateTime?)history.CreatedAt) < toDateExclusive);
        }

        var page = Math.Max(request.Page, PaginationRequest.DefaultPage);
        var pageSize = request.PageSize < 1
            ? PaginationRequest.DefaultPageSize
            : Math.Min(request.PageSize, PaginationRequest.MaxPageSize);
        var sortDescending = ParseSortOrder(request.SortOrder);

        query = ApplySorting(query, request.SortBy, sortDescending);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(payment => new PaymentListItemResponse
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                PaymentMethod = ApiEnumMapper.ToApiPaymentMethod(payment.PaymentMethod),
                PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PaymentListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<PaymentDetailResponse?> GetPaymentByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Include(current => current.Order)
            .FirstOrDefaultAsync(
                current => current.PaymentId == paymentId && (canAccessAllOrders || current.Order.UserId == currentUserId),
                cancellationToken);

        return payment is null
            ? null
            : new PaymentDetailResponse
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                PaymentMethod = ApiEnumMapper.ToApiPaymentMethod(payment.PaymentMethod),
                PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus),
                PaidAt = payment.PaidAt
            };
    }

    public async Task<PaymentStatusUpdatedResponse> UpdatePaymentStatusAsync(
        int paymentId,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ApiEnumMapper.TryParsePaymentStatus(request.PaymentStatus, out var paymentStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_STATUS", "Invalid payment status update");
        }

        var payment = await _dbContext.Payments
            .Include(current => current.PaymentHistories)
            .FirstOrDefaultAsync(current => current.PaymentId == paymentId, cancellationToken);

        if (payment is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PAYMENT_NOT_FOUND", "Payment not found");
        }

        if (payment.PaymentStatus == paymentStatus)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_STATUS", "Invalid payment status update");
        }

        var now = DateTime.UtcNow;
        payment.PaymentStatus = paymentStatus;
        payment.PaidAt = paymentStatus == PaymentStatus.Completed ? now : null;
        payment.PaymentHistories.Add(new PaymentHistory
        {
            PaymentStatus = paymentStatus,
            TransactionCode = NormalizeText(request.TransactionCode),
            Notes = NormalizeText(request.Notes),
            CreatedAt = now
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentStatusUpdatedResponse
        {
            Message = "Payment status updated",
            PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(paymentStatus)
        };
    }

    public async Task<PaymentHistoriesResponse> GetPaymentHistoriesAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Include(current => current.PaymentHistories)
            .FirstOrDefaultAsync(current => current.PaymentId == paymentId, cancellationToken);

        if (payment is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PAYMENT_NOT_FOUND", "Payment not found");
        }

        return new PaymentHistoriesResponse
        {
            Items = payment.PaymentHistories
                .OrderBy(history => history.CreatedAt)
                .ThenBy(history => history.PaymentHistoryId)
                .Select(history => new PaymentHistoryListItemResponse
                {
                    PaymentHistoryId = history.PaymentHistoryId,
                    PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(history.PaymentStatus),
                    TransactionCode = history.TransactionCode,
                    CreatedAt = history.CreatedAt
                })
                .ToList()
        };
    }

    public async Task<PaymentActionResponse?> InitializePayOsPaymentAsync(
        Payment payment,
        Order order,
        string? orderInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(order);

        if (payment.PaymentMethod != PaymentMethod.PayOS)
        {
            return null;
        }

        var orderCode = BuildPayOsOrderCode();
        PayOsCreatePaymentLinkResult gatewayResult;

        try
        {
            gatewayResult = await _payOsGatewayClient.CreatePaymentLinkAsync(
                new PayOsCreateGatewayRequestDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = order.OrderId,
                    OrderCode = orderCode,
                    Amount = payment.Amount,
                    Description = BuildPaymentReference(payment.PaymentId)
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            await AddPaymentHistoryAsync(
                payment,
                PaymentStatus.Pending,
                null,
                $"PayOS initialization failed: {TruncateNote(exception.Message)}",
                cancellationToken);

            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        await AddPaymentHistoryAsync(
            payment,
            PaymentStatus.Pending,
            gatewayResult.OrderCode.ToString(CultureInfo.InvariantCulture),
            "PayOS payment initialized.",
            cancellationToken);

        return new PaymentActionResponse
        {
            PaymentId = payment.PaymentId,
            PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus),
            PayUrl = gatewayResult.CheckoutUrl,
            Deeplink = null,
            QrCodeUrl = gatewayResult.QrCode
        };
    }

    public async Task<PayOsWebhookResponse> HandlePayOsWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return CreateWebhookResponse(false, true, "Invalid webhook data");
        }

        var envelope = ParseWebhookEnvelope(rawPayload);
        var verification = _payOsGatewayClient.VerifyWebhookPayload(rawPayload);

        if (verification.Status == PayOsWebhookVerificationStatus.InvalidSignature)
        {
            return CreateWebhookResponse(false, false, "Invalid webhook signature");
        }

        if (verification.Status != PayOsWebhookVerificationStatus.Valid || verification.Data is null)
        {
            return CreateWebhookResponse(false, true, "Invalid webhook data");
        }

        var resolution = await ResolvePayOsWebhookAsync(verification.Data, cancellationToken);

        if (resolution.Result == PayOsWebhookResolutionResult.PaymentNotFound || resolution.Payment is null)
        {
            return CreateWebhookResponse(false, true, "Payment not found");
        }

        if (resolution.Result == PayOsWebhookResolutionResult.InvalidAmount)
        {
            return CreateWebhookResponse(false, false, "Invalid amount");
        }

        var successfulWebhook = IsPayOsSuccess(envelope);

        await ApplyPayOsWebhookResultAsync(
            resolution.Payment,
            verification.Data,
            successfulWebhook,
            cancellationToken);

        return successfulWebhook
            ? CreateWebhookResponse(true, true, "Payment processed successfully")
            : CreateWebhookResponse(true, true, "Payment failed recorded");
    }

    private async Task AddPaymentHistoryAsync(
        Payment payment,
        PaymentStatus paymentStatus,
        string? transactionCode,
        string notes,
        CancellationToken cancellationToken)
    {
        payment.PaymentHistories.Add(new PaymentHistory
        {
            PaymentStatus = paymentStatus,
            TransactionCode = TruncateTransactionCode(transactionCode),
            Notes = TruncateNote(notes),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<PayOsWebhookResolution> ResolvePayOsWebhookAsync(
        PayOsVerifiedWebhookData webhookData,
        CancellationToken cancellationToken)
    {
        if (!TryResolvePaymentId(webhookData, out var paymentId))
        {
            return new PayOsWebhookResolution(PayOsWebhookResolutionResult.PaymentNotFound, null);
        }

        var payment = await _dbContext.Payments
            .Include(current => current.PaymentHistories)
            .FirstOrDefaultAsync(
                current => current.PaymentId == paymentId && current.PaymentMethod == PaymentMethod.PayOS,
                cancellationToken);

        if (payment is null && webhookData.OrderCode > 0)
        {
            var orderCodeText = webhookData.OrderCode.ToString(CultureInfo.InvariantCulture);
            payment = await _dbContext.Payments
                .Include(current => current.PaymentHistories)
                .FirstOrDefaultAsync(
                    current => current.PaymentMethod == PaymentMethod.PayOS
                        && current.PaymentHistories.Any(history => history.TransactionCode == orderCodeText),
                    cancellationToken);
        }

        if (payment is null)
        {
            return new PayOsWebhookResolution(PayOsWebhookResolutionResult.PaymentNotFound, null);
        }

        if (webhookData.Amount <= 0 || webhookData.Amount != ToPayOsAmount(payment.Amount))
        {
            return new PayOsWebhookResolution(PayOsWebhookResolutionResult.InvalidAmount, null);
        }

        return new PayOsWebhookResolution(PayOsWebhookResolutionResult.Valid, payment);
    }

    private async Task ApplyPayOsWebhookResultAsync(
        Payment payment,
        PayOsVerifiedWebhookData webhookData,
        bool success,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var transactionCode = TruncateTransactionCode(
            NormalizeText(webhookData.Reference)
            ?? webhookData.OrderCode.ToString(CultureInfo.InvariantCulture));

        if (success)
        {
            if (payment.PaymentStatus == PaymentStatus.Completed)
            {
                return;
            }

            payment.PaymentStatus = PaymentStatus.Completed;
            payment.PaidAt = now;
            payment.PaymentHistories.Add(new PaymentHistory
            {
                PaymentStatus = PaymentStatus.Completed,
                TransactionCode = transactionCode,
                Notes = "PayOS webhook confirmed payment.",
                CreatedAt = now
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (payment.PaymentStatus == PaymentStatus.Completed || payment.PaymentStatus == PaymentStatus.Failed)
        {
            return;
        }

        payment.PaymentStatus = PaymentStatus.Failed;
        payment.PaidAt = null;
        payment.PaymentHistories.Add(new PaymentHistory
        {
            PaymentStatus = PaymentStatus.Failed,
            TransactionCode = transactionCode,
            Notes = "PayOS webhook marked payment as failed.",
            CreatedAt = now
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool TryResolvePaymentId(PayOsVerifiedWebhookData webhookData, out int paymentId)
    {
        paymentId = 0;

        if (TryParsePaymentReference(NormalizeText(webhookData.Description), out paymentId))
        {
            return true;
        }

        if (TryParsePaymentReference(NormalizeText(webhookData.Reference), out paymentId))
        {
            return true;
        }

        return false;
    }

    private static bool IsPayOsSuccess(PayOsWebhookEnvelope envelope)
    {
        var topLevelSuccess = string.Equals(envelope.Code, "00", StringComparison.OrdinalIgnoreCase)
            && (!envelope.Success.HasValue || envelope.Success.Value);
        var dataLevelSuccess = string.Equals(envelope.DataCode, "00", StringComparison.OrdinalIgnoreCase);

        return topLevelSuccess || dataLevelSuccess;
    }

    private static PayOsWebhookEnvelope ParseWebhookEnvelope(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            var code = ReadJsonString(root, "code");
            var success = ReadJsonBoolean(root, "success");
            var dataCode = default(string);

            if (TryReadJsonObject(root, "data", out var dataElement))
            {
                dataCode = ReadJsonString(dataElement, "code");
            }

            return new PayOsWebhookEnvelope
            {
                Code = code,
                Success = success,
                DataCode = dataCode
            };
        }
        catch
        {
            return new PayOsWebhookEnvelope();
        }
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        if (!TryReadJsonProperty(element, propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind == JsonValueKind.String
            ? NormalizeText(propertyValue.GetString())
            : NormalizeText(propertyValue.ToString());
    }

    private static bool? ReadJsonBoolean(JsonElement element, string propertyName)
    {
        if (!TryReadJsonProperty(element, propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => bool.TryParse(propertyValue.ToString(), out var parsed) ? parsed : null
        };
    }

    private static bool TryReadJsonObject(JsonElement element, string propertyName, out JsonElement objectElement)
    {
        objectElement = default;

        if (!TryReadJsonProperty(element, propertyName, out var propertyValue)
            || propertyValue.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        objectElement = propertyValue;
        return true;
    }

    private static bool TryReadJsonProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static IOrderedQueryable<Payment> ApplySorting(IQueryable<Payment> query, string? sortBy, bool descending)
    {
        var normalizedSortBy = NormalizeSortField(sortBy);

        return normalizedSortBy switch
        {
            null or "createdat" => descending
                ? query.OrderByDescending(payment => payment.PaymentHistories.Min(history => (DateTime?)history.CreatedAt))
                    .ThenByDescending(payment => payment.PaymentId)
                : query.OrderBy(payment => payment.PaymentHistories.Min(history => (DateTime?)history.CreatedAt))
                    .ThenBy(payment => payment.PaymentId),
            "paymentid" => descending
                ? query.OrderByDescending(payment => payment.PaymentId)
                : query.OrderBy(payment => payment.PaymentId),
            "orderid" => descending
                ? query.OrderByDescending(payment => payment.OrderId)
                : query.OrderBy(payment => payment.OrderId),
            "amount" => descending
                ? query.OrderByDescending(payment => payment.Amount).ThenByDescending(payment => payment.PaymentId)
                : query.OrderBy(payment => payment.Amount).ThenBy(payment => payment.PaymentId),
            "paidat" => descending
                ? query.OrderByDescending(payment => payment.PaidAt).ThenByDescending(payment => payment.PaymentId)
                : query.OrderBy(payment => payment.PaidAt).ThenBy(payment => payment.PaymentId),
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid payment query")
        };
    }

    private static bool ParseSortOrder(string? sortOrder)
    {
        var normalizedSortOrder = NormalizeSortField(sortOrder);

        return normalizedSortOrder switch
        {
            null or "desc" => true,
            "asc" => false,
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid payment query")
        };
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

    private static string BuildPaymentReference(int paymentId)
    {
        return $"PAYMENT_{paymentId}";
    }

    private static bool TryParsePaymentReference(string? reference, out int paymentId)
    {
        paymentId = 0;

        if (string.IsNullOrWhiteSpace(reference) || !reference.StartsWith("PAYMENT_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(reference["PAYMENT_".Length..], out paymentId) && paymentId > 0;
    }

    private static bool IsSupportedPaymentMethod(PaymentMethod paymentMethod)
    {
        return paymentMethod == PaymentMethod.COD || paymentMethod == PaymentMethod.PayOS;
    }

    private static int ToPayOsAmount(decimal amount)
    {
        return Convert.ToInt32(Math.Round(amount, 0, MidpointRounding.AwayFromZero));
    }

    private static long BuildPayOsOrderCode()
    {
        var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var entropy = RandomNumberGenerator.GetInt32(100, 999);
        return (milliseconds * 1000L) + entropy;
    }

    private static string TruncateNote(string? value)
    {
        var normalizedValue = NormalizeText(value) ?? string.Empty;
        return normalizedValue.Length <= 255 ? normalizedValue : normalizedValue[..255];
    }

    private static string? TruncateTransactionCode(string? value)
    {
        var normalizedValue = NormalizeText(value);

        if (normalizedValue is null)
        {
            return null;
        }

        return normalizedValue.Length <= 100 ? normalizedValue : normalizedValue[..100];
    }

    private static PayOsWebhookResponse CreateWebhookResponse(bool success, bool acknowledged, string message)
    {
        return new PayOsWebhookResponse
        {
            Success = success,
            Acknowledged = acknowledged,
            Message = message
        };
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string errorCode, string message, object? details = null)
    {
        return new ApiException((int)statusCode, errorCode, message, details);
    }

    private enum PayOsWebhookResolutionResult
    {
        Valid = 1,
        PaymentNotFound = 2,
        InvalidAmount = 3
    }

    private sealed class PayOsWebhookEnvelope
    {
        public string? Code { get; set; }

        public bool? Success { get; set; }

        public string? DataCode { get; set; }
    }

    private sealed record PayOsWebhookResolution(
        PayOsWebhookResolutionResult Result,
        Payment? Payment);
}
