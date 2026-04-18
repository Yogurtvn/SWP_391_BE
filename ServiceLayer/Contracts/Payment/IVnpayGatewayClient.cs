using ServiceLayer.DTOs.Payment.Request;

namespace ServiceLayer.Contracts.Payment;

public interface IVnpayGatewayClient
{
    string CreatePaymentUrl(VnpayCreateGatewayRequestDto request);

    bool ValidateSignature(IReadOnlyDictionary<string, string> queryParameters);

    bool IsValidTmnCode(string? tmnCode);
}
