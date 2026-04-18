using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;

namespace ServiceLayer.Contracts.Payment;

public interface IMomoGatewayClient
{
    Task<MomoCreateGatewayResultDto> CreatePaymentAsync(MomoCreateGatewayRequestDto request, CancellationToken cancellationToken = default);

    bool ValidateSignature(MomoIpnRequest request);

    bool IsValidPartnerCode(string partnerCode);
}
