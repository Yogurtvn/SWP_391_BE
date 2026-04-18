using RepositoryLayer.Common;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using PaymentEntity = RepositoryLayer.Entities.Payment;
using OrderEntity = RepositoryLayer.Entities.Order;

namespace ServiceLayer.Contracts.Payment;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(
        int currentUserId,
        bool canAccessAllOrders,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PaymentListItemResponse>> GetPaymentsAsync(
        GetPaymentsRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentDetailResponse?> GetPaymentByIdAsync(
        int currentUserId,
        bool canAccessAllOrders,
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentDetailResponse?> GetPaymentByPayOsOrderCodeAsync(
        int currentUserId,
        bool canAccessAllOrders,
        long orderCode,
        CancellationToken cancellationToken = default);

    Task<PayOsPaymentReconciliationResponse> ReconcilePayOsPaymentAsync(
        int currentUserId,
        bool canAccessAllOrders,
        long orderCode,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusUpdatedResponse> UpdatePaymentStatusAsync(
        int paymentId,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentHistoriesResponse> GetPaymentHistoriesAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentActionResponse?> InitializePayOsPaymentAsync(
        PaymentEntity payment,
        OrderEntity order,
        string? orderInfo,
        CancellationToken cancellationToken = default);

    Task<PayOsWebhookResponse> HandlePayOsWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken = default);
}
