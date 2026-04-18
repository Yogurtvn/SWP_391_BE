using Microsoft.Extensions.Configuration;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ServiceLayer.Services.PaymentManagement;

public class MomoGatewayClient(
    IConfiguration configuration) : IMomoGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MomoApiOptions _options = BuildOptions(configuration);

    public async Task<MomoCreateGatewayResultDto> CreatePaymentAsync(
        MomoCreateGatewayRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var amount = ToLongAmount(request.Amount);

        if (amount < 1000)
        {
            throw new InvalidOperationException("MoMo requires an amount of at least 1000 VND.");
        }

        var orderInfo = string.IsNullOrWhiteSpace(request.OrderInfo)
            ? $"Thanh toan don hang {request.OrderId}"
            : request.OrderInfo.Trim();

        orderInfo = orderInfo.Replace("&", " ", StringComparison.Ordinal);

        var extraData = BuildExtraData(request.PaymentId, request.OrderId);
        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}" +
            $"&ipnUrl={_options.IpnUrl}&orderId={request.OrderReference}&orderInfo={orderInfo}" +
            $"&partnerCode={_options.PartnerCode}&redirectUrl={_options.RedirectUrl}" +
            $"&requestId={request.OrderReference}&requestType=captureWallet";

        var signature = ComputeHmacSha256(rawSignature, _options.SecretKey);
        var payload = new MomoCreateGatewayPayload
        {
            PartnerCode = _options.PartnerCode,
            RequestType = "captureWallet",
            IpnUrl = _options.IpnUrl,
            RedirectUrl = _options.RedirectUrl,
            OrderId = request.OrderReference,
            Amount = amount,
            OrderInfo = orderInfo,
            RequestId = request.OrderReference,
            ExtraData = extraData,
            Lang = "vi",
            Signature = signature
        };

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var httpResponse = await client.PostAsJsonAsync(_options.ApiUrl, payload, cancellationToken);
        var rawResponse = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var momoResponse = JsonSerializer.Deserialize<MomoCreateGatewayResponse>(rawResponse, JsonOptions);

        return new MomoCreateGatewayResultDto
        {
            IsSuccessStatusCode = httpResponse.IsSuccessStatusCode,
            HttpStatusCode = (int)httpResponse.StatusCode,
            RawResponse = rawResponse,
            ResultCode = momoResponse?.ResultCode ?? -1,
            Message = momoResponse?.Message ?? string.Empty,
            PayUrl = momoResponse?.PayUrl,
            Deeplink = momoResponse?.Deeplink,
            QrCodeUrl = momoResponse?.QrCodeUrl
        };
    }

    public bool ValidateSignature(MomoIpnRequest request)
    {
        ValidateConfiguration();

        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={request.Amount}&extraData={request.ExtraData}" +
            $"&message={request.Message}&orderId={request.OrderId}&orderInfo={request.OrderInfo}" +
            $"&orderType={request.OrderType}&partnerCode={request.PartnerCode}&payType={request.PayType}" +
            $"&requestId={request.RequestId}&responseTime={request.ResponseTime}" +
            $"&resultCode={request.ResultCode}&transId={request.TransId}";

        var computedSignature = ComputeHmacSha256(rawSignature, _options.SecretKey);
        return string.Equals(computedSignature, request.Signature, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValidPartnerCode(string partnerCode)
    {
        ValidateConfiguration();
        return string.Equals(partnerCode, _options.PartnerCode, StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiUrl)
            || string.IsNullOrWhiteSpace(_options.PartnerCode)
            || string.IsNullOrWhiteSpace(_options.AccessKey)
            || string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.RedirectUrl)
            || string.IsNullOrWhiteSpace(_options.IpnUrl)
            || _options.ApiUrl.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.RedirectUrl.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.IpnUrl.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MomoAPI configuration is missing or still using placeholders.");
        }
    }

    private static MomoApiOptions BuildOptions(IConfiguration configuration)
    {
        return new MomoApiOptions
        {
            ApiUrl = configuration["MomoAPI:ApiUrl"] ?? string.Empty,
            PartnerCode = configuration["MomoAPI:PartnerCode"] ?? string.Empty,
            AccessKey = configuration["MomoAPI:AccessKey"] ?? string.Empty,
            SecretKey = configuration["MomoAPI:SecretKey"] ?? string.Empty,
            RedirectUrl = configuration["MomoAPI:RedirectUrl"] ?? string.Empty,
            IpnUrl = configuration["MomoAPI:IpnUrl"] ?? string.Empty
        };
    }

    private static string BuildExtraData(int paymentId, int orderId)
    {
        var payload = JsonSerializer.Serialize(new { paymentId, orderId });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    private static string ComputeHmacSha256(string rawData, string secretKey)
    {
        var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        var rawDataBytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA256(secretKeyBytes);
        var hashBytes = hmac.ComputeHash(rawDataBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static long ToLongAmount(decimal amount)
    {
        return Convert.ToInt64(Math.Round(amount, 0, MidpointRounding.AwayFromZero));
    }

    private sealed class MomoCreateGatewayPayload
    {
        public string PartnerCode { get; set; } = string.Empty;

        public string RequestType { get; set; } = string.Empty;

        public string IpnUrl { get; set; } = string.Empty;

        public string RedirectUrl { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;

        public long Amount { get; set; }

        public string OrderInfo { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public string ExtraData { get; set; } = string.Empty;

        public string Lang { get; set; } = "vi";

        public string Signature { get; set; } = string.Empty;
    }

    private sealed class MomoCreateGatewayResponse
    {
        public int ResultCode { get; set; }

        public string? Message { get; set; }

        public string? PayUrl { get; set; }

        public string? Deeplink { get; set; }

        public string? QrCodeUrl { get; set; }
    }
}
