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
using System.Net;

namespace ServiceLayer.Services.PaymentManagement;

public class PaymentService(
    IUnitOfWork unitOfWork,
    OnlineEyewearDbContext dbContext,
    IVnpayGatewayClient vnpayGatewayClient) : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;
    private readonly IVnpayGatewayClient _vnpayGatewayClient = vnpayGatewayClient;

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

        var paymentAction = payment.PaymentMethod == PaymentMethod.VNPay
            ? await InitializeVnpayPaymentAsync(payment, order, null, cancellationToken)
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

    public async Task<PaymentActionResponse?> InitializeVnpayPaymentAsync(
        Payment payment,
        Order order,
        string? orderInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(order);

        if (payment.PaymentMethod != PaymentMethod.VNPay)
        {
            return null;
        }

        string payUrl;

        try
        {
            payUrl = _vnpayGatewayClient.CreatePaymentUrl(
                new VnpayCreateGatewayRequestDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = order.OrderId,
                    OrderReference = BuildPaymentReference(payment.PaymentId),
                    Amount = payment.Amount,
                    OrderInfo = orderInfo
                });
        }
        catch (Exception exception)
        {
            await AddPaymentHistoryAsync(
                payment,
                PaymentStatus.Pending,
                null,
                $"VNPay initialization failed: {TruncateNote(exception.Message)}",
                cancellationToken);

            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PAYMENT_REQUEST", "Cannot create payment");
        }

        await AddPaymentHistoryAsync(payment, PaymentStatus.Pending, null, "VNPay payment initialized.", cancellationToken);

        return new PaymentActionResponse
        {
            PaymentId = payment.PaymentId,
            PaymentStatus = ApiEnumMapper.ToApiPaymentStatus(payment.PaymentStatus),
            PayUrl = payUrl,
            Deeplink = null,
            QrCodeUrl = null
        };
    }

    public async Task HandleVnpayReturnAsync(
        IReadOnlyDictionary<string, string> queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var resolution = await ResolveVnpayCallbackAsync(queryParameters, cancellationToken);

        if (resolution.Result != VnpayCallbackResolutionResult.Valid || resolution.Payment is null)
        {
            return;
        }

        await ApplyVnpayCallbackResultAsync(
            resolution.Payment,
            queryParameters,
            "VNPay return",
            cancellationToken);
    }

    public async Task<VnpayIpnResponse> HandleVnpayIpnAsync(
        IReadOnlyDictionary<string, string> queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var resolution = await ResolveVnpayCallbackAsync(queryParameters, cancellationToken);

        if (resolution.Result == VnpayCallbackResolutionResult.InvalidSignature)
        {
            return new VnpayIpnResponse
            {
                RspCode = "97",
                Message = "Invalid Signature"
            };
        }

        if (resolution.Result == VnpayCallbackResolutionResult.PaymentNotFound)
        {
            return new VnpayIpnResponse
            {
                RspCode = "01",
                Message = "Payment Not Found"
            };
        }

        if (resolution.Result == VnpayCallbackResolutionResult.InvalidAmount)
        {
            return new VnpayIpnResponse
            {
                RspCode = "04",
                Message = "Invalid Amount"
            };
        }

        if (resolution.Payment is null)
        {
            return new VnpayIpnResponse
            {
                RspCode = "99",
                Message = "Unknown Error"
            };
        }

        await ApplyVnpayCallbackResultAsync(
            resolution.Payment,
            queryParameters,
            "VNPay IPN",
            cancellationToken);

        return new VnpayIpnResponse
        {
            RspCode = "00",
            Message = "Confirm Success"
        };
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
            TransactionCode = transactionCode,
            Notes = TruncateNote(notes),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<VnpayCallbackResolution> ResolveVnpayCallbackAsync(
        IReadOnlyDictionary<string, string> queryParameters,
        CancellationToken cancellationToken)
    {
        if (!_vnpayGatewayClient.ValidateSignature(queryParameters)
            || !_vnpayGatewayClient.IsValidTmnCode(GetQueryValue(queryParameters, "vnp_TmnCode")))
        {
            return new VnpayCallbackResolution(VnpayCallbackResolutionResult.InvalidSignature, null);
        }

        if (!TryParsePaymentReference(GetQueryValue(queryParameters, "vnp_TxnRef"), out var paymentId))
        {
            return new VnpayCallbackResolution(VnpayCallbackResolutionResult.PaymentNotFound, null);
        }

        var payment = await _dbContext.Payments
            .Include(current => current.PaymentHistories)
            .FirstOrDefaultAsync(
                current => current.PaymentId == paymentId && current.PaymentMethod == PaymentMethod.VNPay,
                cancellationToken);

        if (payment is null)
        {
            return new VnpayCallbackResolution(VnpayCallbackResolutionResult.PaymentNotFound, null);
        }

        var callbackAmount = GetQueryValue(queryParameters, "vnp_Amount");

        if (!long.TryParse(callbackAmount, out var gatewayAmount)
            || gatewayAmount != ToVnpayAmount(payment.Amount))
        {
            return new VnpayCallbackResolution(VnpayCallbackResolutionResult.InvalidAmount, null);
        }

        return new VnpayCallbackResolution(VnpayCallbackResolutionResult.Valid, payment);
    }

    private async Task ApplyVnpayCallbackResultAsync(
        Payment payment,
        IReadOnlyDictionary<string, string> queryParameters,
        string source,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var transactionCode = TruncateTransactionCode(GetQueryValue(queryParameters, "vnp_TransactionNo"));

        if (IsVnpaySuccess(queryParameters))
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
                Notes = $"{source} confirmed payment.",
                CreatedAt = now
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (payment.PaymentStatus == PaymentStatus.Completed)
        {
            return;
        }

        var responseCode = GetQueryValue(queryParameters, "vnp_ResponseCode") ?? "unknown";
        var transactionStatus = GetQueryValue(queryParameters, "vnp_TransactionStatus") ?? "unknown";

        payment.PaymentStatus = PaymentStatus.Failed;
        payment.PaidAt = null;
        payment.PaymentHistories.Add(new PaymentHistory
        {
            PaymentStatus = PaymentStatus.Failed,
            TransactionCode = transactionCode,
            Notes = TruncateNote($"{source} failed: responseCode={responseCode}, transactionStatus={transactionStatus}."),
            CreatedAt = now
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? GetQueryValue(IReadOnlyDictionary<string, string> queryParameters, string key)
    {
        return queryParameters.TryGetValue(key, out var value)
            ? NormalizeText(value)
            : null;
    }

    private static bool IsVnpaySuccess(IReadOnlyDictionary<string, string> queryParameters)
    {
        var responseCode = GetQueryValue(queryParameters, "vnp_ResponseCode");
        var transactionStatus = GetQueryValue(queryParameters, "vnp_TransactionStatus");

        return string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase)
            && (transactionStatus is null || string.Equals(transactionStatus, "00", StringComparison.OrdinalIgnoreCase));
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
        return paymentMethod == PaymentMethod.COD || paymentMethod == PaymentMethod.VNPay;
    }

    private static long ToVnpayAmount(decimal amount)
    {
        return Convert.ToInt64(Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
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

    private static ApiException CreateApiException(HttpStatusCode statusCode, string errorCode, string message, object? details = null)
    {
        return new ApiException((int)statusCode, errorCode, message, details);
    }

    private enum VnpayCallbackResolutionResult
    {
        Valid,
        InvalidSignature,
        PaymentNotFound,
        InvalidAmount
    }

    private sealed record VnpayCallbackResolution(
        VnpayCallbackResolutionResult Result,
        Payment? Payment);
}
