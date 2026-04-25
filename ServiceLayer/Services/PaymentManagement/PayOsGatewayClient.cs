using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ServiceLayer.Services.PaymentManagement;

public class PayOsGatewayClient(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IPayOsGatewayClient
{
    private const string PayOsMerchantApiBaseUrl = "https://api-merchant.payos.vn";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PayOsApiOptions _options = BuildOptions(configuration);

    private readonly PayOS _payOs = new(
        NormalizeSecret(configuration["PayOS:ClientId"], "PayOS:ClientId"),
        NormalizeSecret(configuration["PayOS:ApiKey"], "PayOS:ApiKey"),
        NormalizeSecret(configuration["PayOS:ChecksumKey"], "PayOS:ChecksumKey"));

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<PayOsCreatePaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreateGatewayRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        if (request.OrderCode <= 0)
        {
            throw new InvalidOperationException("PayOS requires a positive orderCode.");
        }

        var amount = ToPayOsAmount(request.Amount);
        if (amount <= 0)
        {
            throw new InvalidOperationException("PayOS requires an amount greater than 0 VND.");
        }

        var description = NormalizeDescription(request.Description, request.PaymentId);
        var items = new List<ItemData>
        {
            new($"Thanh toán đơn hàng {request.OrderId}", 1, amount)
        };

        var paymentData = new PaymentData(
            orderCode: request.OrderCode,
            amount: amount,
            description: description,
            items: items,
            cancelUrl: _options.CancelUrl,
            returnUrl: _options.ReturnUrl);

        var response = await _payOs.createPaymentLink(paymentData);

        cancellationToken.ThrowIfCancellationRequested();

        return new PayOsCreatePaymentLinkResult
        {
            OrderCode = request.OrderCode,
            CheckoutUrl = response.checkoutUrl,
            QrCode = response.qrCode
        };
    }

    public async Task<PayOsPaymentLinkInformationResult> GetPaymentLinkInformationAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        if (orderCode <= 0)
        {
            throw new InvalidOperationException("PayOS requires a positive orderCode.");
        }

        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(PayOsMerchantApiBaseUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
        client.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

        using var response = await client.GetAsync($"/v2/payment-requests/{orderCode}", cancellationToken);
        var rawPayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"PayOS payment lookup failed with status {(int)response.StatusCode}: {Truncate(rawPayload, 255)}");
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            var code = ReadJsonString(root, "code");

            if (!string.Equals(code, "00", StringComparison.OrdinalIgnoreCase))
            {
                var desc = ReadJsonString(root, "desc") ?? "PayOS payment lookup failed.";
                throw new InvalidOperationException(Truncate(desc, 255));
            }

            if (!TryReadJsonObject(root, "data", out var dataElement))
            {
                throw new InvalidOperationException("PayOS payment lookup returned invalid data.");
            }

            return new PayOsPaymentLinkInformationResult
            {
                OrderCode = ReadJsonLong(dataElement, "orderCode"),
                PaymentLinkId = ReadJsonString(dataElement, "id") ?? string.Empty,
                Amount = ReadJsonInt(dataElement, "amount"),
                AmountPaid = ReadJsonInt(dataElement, "amountPaid"),
                AmountRemaining = ReadJsonInt(dataElement, "amountRemaining"),
                Status = ReadJsonString(dataElement, "status") ?? string.Empty
            };
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"PayOS payment lookup returned malformed JSON: {Truncate(exception.Message, 255)}");
        }
    }

    public PayOsWebhookVerificationResult VerifyWebhookPayload(string rawPayload)
    {
        ValidateConfiguration();

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return new PayOsWebhookVerificationResult
            {
                Status = PayOsWebhookVerificationStatus.InvalidPayload,
                Message = "Webhook payload is empty."
            };
        }

        var verifyMethod = _payOs
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(method =>
                string.Equals(method.Name, "verifyPaymentWebhookData", StringComparison.Ordinal)
                && method.GetParameters().Length == 1);

        if (verifyMethod is null)
        {
            return new PayOsWebhookVerificationResult
            {
                Status = PayOsWebhookVerificationStatus.InvalidPayload,
                Message = "PayOS SDK verification method is unavailable."
            };
        }

        try
        {
            var parameterType = verifyMethod.GetParameters()[0].ParameterType;
            var webhookObject = JsonSerializer.Deserialize(rawPayload, parameterType, JsonOptions);

            if (webhookObject is null)
            {
                return new PayOsWebhookVerificationResult
                {
                    Status = PayOsWebhookVerificationStatus.InvalidPayload,
                    Message = "Webhook payload is invalid."
                };
            }

            var verifiedDataObject = verifyMethod.Invoke(_payOs, [webhookObject]);
            if (verifiedDataObject is null)
            {
                return new PayOsWebhookVerificationResult
                {
                    Status = PayOsWebhookVerificationStatus.InvalidPayload,
                    Message = "PayOS webhook payload is invalid."
                };
            }

            return new PayOsWebhookVerificationResult
            {
                Status = PayOsWebhookVerificationStatus.Valid,
                Message = "Webhook verified.",
                Data = new PayOsVerifiedWebhookData
                {
                    OrderCode = ReadLongProperty(verifiedDataObject, "orderCode"),
                    Amount = ReadIntProperty(verifiedDataObject, "amount"),
                    Description = ReadStringProperty(verifiedDataObject, "description"),
                    Reference = ReadStringProperty(verifiedDataObject, "reference")
                }
            };
        }
        catch (TargetInvocationException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;

            return new PayOsWebhookVerificationResult
            {
                Status = IsSignatureError(message)
                    ? PayOsWebhookVerificationStatus.InvalidSignature
                    : PayOsWebhookVerificationStatus.InvalidPayload,
                Message = Truncate(message, 255)
            };
        }
        catch (Exception exception)
        {
            return new PayOsWebhookVerificationResult
            {
                Status = PayOsWebhookVerificationStatus.InvalidPayload,
                Message = Truncate(exception.Message, 255)
            };
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ApiKey)
            || string.IsNullOrWhiteSpace(_options.ChecksumKey)
            || string.IsNullOrWhiteSpace(_options.ReturnUrl)
            || string.IsNullOrWhiteSpace(_options.CancelUrl)
            || _options.ClientId.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.ApiKey.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.ChecksumKey.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.ReturnUrl.StartsWith("__SET_", StringComparison.Ordinal)
            || _options.CancelUrl.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PayOS configuration is missing or still using placeholders.");
        }
    }

    private static PayOsApiOptions BuildOptions(IConfiguration configuration)
    {
        return new PayOsApiOptions
        {
            ClientId = NormalizeSecret(configuration["PayOS:ClientId"], "PayOS:ClientId"),
            ApiKey = NormalizeSecret(configuration["PayOS:ApiKey"], "PayOS:ApiKey"),
            ChecksumKey = NormalizeSecret(configuration["PayOS:ChecksumKey"], "PayOS:ChecksumKey"),
            ReturnUrl = NormalizeUrl(configuration["PayOS:ReturnUrl"]),
            CancelUrl = NormalizeUrl(configuration["PayOS:CancelUrl"])
        };
    }

    private static long ReadLongProperty(object source, string propertyName)
    {
        var rawValue = ReadProperty(source, propertyName);

        return rawValue switch
        {
            null => 0L,
            long value => value,
            int value => value,
            decimal value => Convert.ToInt64(value),
            _ => long.TryParse(rawValue.ToString(), out var parsed) ? parsed : 0L
        };
    }

    private static int ReadIntProperty(object source, string propertyName)
    {
        var rawValue = ReadProperty(source, propertyName);

        return rawValue switch
        {
            null => 0,
            int value => value,
            long value when value is >= int.MinValue and <= int.MaxValue => (int)value,
            decimal value => Convert.ToInt32(value),
            _ => int.TryParse(rawValue.ToString(), out var parsed) ? parsed : 0
        };
    }

    private static string? ReadStringProperty(object source, string propertyName)
    {
        var rawValue = ReadProperty(source, propertyName)?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(rawValue) ? null : rawValue;
    }

    private static object? ReadProperty(object source, string propertyName)
    {
        var property = source
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(current =>
                string.Equals(current.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        return property?.GetValue(source);
    }

    private static bool IsSignatureError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("signature", StringComparison.OrdinalIgnoreCase);
    }

    private static int ToPayOsAmount(decimal amount)
    {
        return Convert.ToInt32(Math.Round(amount, 0, MidpointRounding.AwayFromZero));
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        if (!TryReadJsonProperty(element, propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind == JsonValueKind.String
            ? propertyValue.GetString()?.Trim()
            : propertyValue.ToString()?.Trim();
    }

    private static int ReadJsonInt(JsonElement element, string propertyName)
    {
        if (!TryReadJsonProperty(element, propertyName, out var propertyValue))
        {
            return 0;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.Number when propertyValue.TryGetInt32(out var value) => value,
            JsonValueKind.Number when propertyValue.TryGetInt64(out var longValue)
                && longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            _ => int.TryParse(propertyValue.ToString(), out var parsed) ? parsed : 0
        };
    }

    private static long ReadJsonLong(JsonElement element, string propertyName)
    {
        if (!TryReadJsonProperty(element, propertyName, out var propertyValue))
        {
            return 0L;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.Number when propertyValue.TryGetInt64(out var value) => value,
            _ => long.TryParse(propertyValue.ToString(), out var parsed) ? parsed : 0L
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

    private static string NormalizeDescription(string? description, int paymentId)
    {
        var normalized = description?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = $"PAYMENT_{paymentId}";
        }

        normalized = normalized
            .Replace("&", " ", StringComparison.Ordinal)
            .Replace("#", string.Empty, StringComparison.Ordinal);

        return normalized.Length <= 25 ? normalized : normalized[..25];
    }

    private static string NormalizeSecret(string? value, string keyName)
    {
        var normalized = NormalizeConfigValue(value, removeAllWhitespace: true);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"Configuration value '{keyName}' is missing.");
        }

        return normalized;
    }

    private static string NormalizeUrl(string? value)
    {
        return NormalizeConfigValue(value, removeAllWhitespace: false);
    }

    private static string NormalizeConfigValue(string? value, bool removeAllWhitespace)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            var isHiddenCharacter =
                character == '\u200B'
                || character == '\u200C'
                || character == '\u200D'
                || character == '\u2060'
                || character == '\uFEFF';

            if (isHiddenCharacter)
            {
                continue;
            }

            if (removeAllWhitespace && char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(character);
        }

        return removeAllWhitespace
            ? builder.ToString()
            : builder.ToString().Trim();
    }

    private static string Truncate(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
