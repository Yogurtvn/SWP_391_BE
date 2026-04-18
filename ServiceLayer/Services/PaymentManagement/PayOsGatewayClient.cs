using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Payment;
using ServiceLayer.DTOs.Payment.Request;
using ServiceLayer.DTOs.Payment.Response;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ServiceLayer.Services.PaymentManagement;

public class PayOsGatewayClient(
    IConfiguration configuration) : IPayOsGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PayOsApiOptions _options = BuildOptions(configuration);

    private readonly PayOS _payOs = new(
        NormalizeSecret(configuration["PayOS:ClientId"], "PayOS:ClientId"),
        NormalizeSecret(configuration["PayOS:ApiKey"], "PayOS:ApiKey"),
        NormalizeSecret(configuration["PayOS:ChecksumKey"], "PayOS:ChecksumKey"));

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
            new($"Thanh toan don hang {request.OrderId}", 1, amount)
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
