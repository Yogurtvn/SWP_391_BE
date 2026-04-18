using Microsoft.Extensions.Configuration;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ServiceLayer.Services.PaymentManagement;

public class VnpayGatewayClient(
    IConfiguration configuration) : IVnpayGatewayClient
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private readonly VnpayApiOptions _options = BuildOptions(configuration);

    public string CreatePaymentUrl(VnpayCreateGatewayRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        var amount = ToGatewayAmount(request.Amount);

        if (amount <= 0)
        {
            throw new InvalidOperationException("VNPay requires an amount greater than 0 VND.");
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        var orderInfo = string.IsNullOrWhiteSpace(request.OrderInfo)
            ? $"Thanh toan don hang {request.OrderId}"
            : request.OrderInfo.Trim();
        orderInfo = orderInfo.Replace("&", " ", StringComparison.Ordinal);

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = _options.CurrCode,
            ["vnp_IpAddr"] = NormalizeIpAddress(request.ClientIpAddress),
            ["vnp_Locale"] = _options.Locale,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _options.ReturnUrl,
            ["vnp_TxnRef"] = request.OrderReference,
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(_options.IpnUrl))
        {
            parameters["vnp_IpnUrl"] = _options.IpnUrl;
        }

        var secureHash = ComputeHmacSha512(BuildSignedData(parameters), _options.HashSecret);
        var query = BuildQueryString(parameters);

        return $"{_options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    public bool ValidateSignature(IReadOnlyDictionary<string, string> queryParameters)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);
        ValidateConfiguration();

        if (!queryParameters.TryGetValue("vnp_SecureHash", out var secureHash)
            || string.IsNullOrWhiteSpace(secureHash))
        {
            return false;
        }

        var signedParameters = queryParameters
            .Where(item =>
                item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        var computedHash = ComputeHmacSha512(BuildSignedData(signedParameters), _options.HashSecret);
        return string.Equals(computedHash, secureHash, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValidTmnCode(string? tmnCode)
    {
        ValidateConfiguration();
        return !string.IsNullOrWhiteSpace(tmnCode)
            && string.Equals(tmnCode, _options.TmnCode, StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.TmnCode)
            || string.IsNullOrWhiteSpace(_options.HashSecret)
            || string.IsNullOrWhiteSpace(_options.BaseUrl)
            || string.IsNullOrWhiteSpace(_options.ReturnUrl)
            || string.IsNullOrWhiteSpace(_options.IpnUrl)
            || _options.TmnCode.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.HashSecret.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.BaseUrl.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.ReturnUrl.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.IpnUrl.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("VNPay configuration is missing or still using placeholders.");
        }
    }

    private static VnpayApiOptions BuildOptions(IConfiguration configuration)
    {
        return new VnpayApiOptions
        {
            TmnCode = configuration["VNPay:TmnCode"] ?? string.Empty,
            HashSecret = configuration["VNPay:HashSecret"] ?? string.Empty,
            BaseUrl = configuration["VNPay:BaseUrl"] ?? string.Empty,
            ReturnUrl = configuration["VNPay:ReturnUrl"] ?? string.Empty,
            IpnUrl = configuration["VNPay:IpnUrl"] ?? string.Empty,
            Version = configuration["VNPay:Version"] ?? "2.1.0",
            Command = configuration["VNPay:Command"] ?? "pay",
            CurrCode = configuration["VNPay:CurrCode"] ?? "VND",
            Locale = configuration["VNPay:Locale"] ?? "vn"
        };
    }

    private static string BuildSignedData(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join(
            "&",
            parameters
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={WebUtility.UrlEncode(item.Value)}"));
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join(
            "&",
            parameters
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={WebUtility.UrlEncode(item.Value)}"));
    }

    private static string ComputeHmacSha512(string input, string secretKey)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secretKey);
        var inputBytes = Encoding.UTF8.GetBytes(input);

        using var hmac = new HMACSHA512(secretBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static long ToGatewayAmount(decimal amount)
    {
        return Convert.ToInt64(Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }

    private static string NormalizeIpAddress(string? ipAddress)
    {
        var normalized = ipAddress?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "127.0.0.1";
        }

        var firstIp = normalized
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstIp))
        {
            return "127.0.0.1";
        }

        return firstIp.Length <= 45 ? firstIp : firstIp[..45];
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var timeZoneId in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
