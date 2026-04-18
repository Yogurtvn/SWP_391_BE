using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;

namespace ServiceLayer.Contracts.Payment;

public interface IPayOsGatewayClient
{
    Task<PayOsCreatePaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreateGatewayRequestDto request,
        CancellationToken cancellationToken = default);

    PayOsWebhookVerificationResult VerifyWebhookPayload(string rawPayload);
}
