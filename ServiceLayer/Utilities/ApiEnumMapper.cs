using RepositoryLayer.Enums;

namespace ServiceLayer.Utilities;

internal static class ApiEnumMapper
{
    public static bool TryParseOrderType(string? input, out OrderType orderType)
    {
        orderType = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "ready" => SetValue(OrderType.Ready, out orderType),
            "preorder" => SetValue(OrderType.PreOrder, out orderType),
            "prescription" => SetValue(OrderType.Prescription, out orderType),
            _ => TryParseNumericEnum(normalized, out orderType)
        };
    }

    public static bool TryParseOrderStatus(string? input, out OrderStatus orderStatus)
    {
        orderStatus = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "pending" => SetValue(OrderStatus.Pending, out orderStatus),
            "confirmed" => SetValue(OrderStatus.Confirmed, out orderStatus),
            "awaitingstock" => SetValue(OrderStatus.AwaitingStock, out orderStatus),
            "processing" => SetValue(OrderStatus.Processing, out orderStatus),
            "shipped" => SetValue(OrderStatus.Shipped, out orderStatus),
            "completed" => SetValue(OrderStatus.Completed, out orderStatus),
            "cancelled" => SetValue(OrderStatus.Cancelled, out orderStatus),
            _ => TryParseNumericEnum(normalized, out orderStatus)
        };
    }

    public static bool TryParseShippingStatus(string? input, out ShippingStatus shippingStatus)
    {
        shippingStatus = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "pending" => SetValue(ShippingStatus.Pending, out shippingStatus),
            "picking" => SetValue(ShippingStatus.Picking, out shippingStatus),
            "delivering" => SetValue(ShippingStatus.Delivering, out shippingStatus),
            "delivered" => SetValue(ShippingStatus.Delivered, out shippingStatus),
            "failed" => SetValue(ShippingStatus.Failed, out shippingStatus),
            _ => TryParseNumericEnum(normalized, out shippingStatus)
        };
    }

    public static bool TryParsePaymentMethod(string? input, out PaymentMethod paymentMethod)
    {
        paymentMethod = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "cod" => SetValue(PaymentMethod.COD, out paymentMethod),
            "payos" => SetValue(PaymentMethod.PayOS, out paymentMethod),
            "vnpay" => SetValue(PaymentMethod.PayOS, out paymentMethod), // legacy alias for older FE payloads
            _ => TryParseNumericEnum(normalized, out paymentMethod)
        };
    }

    public static bool TryParsePaymentStatus(string? input, out PaymentStatus paymentStatus)
    {
        paymentStatus = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "pending" => SetValue(PaymentStatus.Pending, out paymentStatus),
            "completed" => SetValue(PaymentStatus.Completed, out paymentStatus),
            "failed" => SetValue(PaymentStatus.Failed, out paymentStatus),
            _ => TryParseNumericEnum(normalized, out paymentStatus)
        };
    }

    public static bool TryParsePrescriptionStatus(string? input, out PrescriptionStatus prescriptionStatus)
    {
        prescriptionStatus = default;
        var normalized = NormalizeEnumToken(input);

        if (normalized is null)
        {
            return false;
        }

        return normalized switch
        {
            "submitted" => SetValue(PrescriptionStatus.Submitted, out prescriptionStatus),
            "reviewing" => SetValue(PrescriptionStatus.Reviewing, out prescriptionStatus),
            "needmoreinfo" => SetValue(PrescriptionStatus.NeedMoreInfo, out prescriptionStatus),
            "approved" => SetValue(PrescriptionStatus.Approved, out prescriptionStatus),
            "rejected" => SetValue(PrescriptionStatus.Rejected, out prescriptionStatus),
            "inproduction" => SetValue(PrescriptionStatus.InProduction, out prescriptionStatus),
            _ => TryParseNumericEnum(normalized, out prescriptionStatus)
        };
    }

    public static string ToApiOrderType(OrderType orderType)
    {
        return orderType switch
        {
            OrderType.Ready => "ready",
            OrderType.PreOrder => "preOrder",
            OrderType.Prescription => "prescription",
            _ => orderType.ToString()
        };
    }

    public static string ToApiOrderStatus(OrderStatus orderStatus)
    {
        return orderStatus switch
        {
            OrderStatus.AwaitingStock => "awaitingStock",
            _ => ToCamelCase(orderStatus.ToString())
        };
    }

    public static string ToApiShippingStatus(ShippingStatus shippingStatus)
    {
        return ToCamelCase(shippingStatus.ToString());
    }

    public static string ToApiPaymentMethod(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.COD => "cod",
            PaymentMethod.PayOS => "payos",
            _ => paymentMethod.ToString()
        };
    }

    public static string ToApiPaymentStatus(PaymentStatus paymentStatus)
    {
        return ToCamelCase(paymentStatus.ToString());
    }

    public static string ToApiPrescriptionStatus(PrescriptionStatus prescriptionStatus)
    {
        return prescriptionStatus switch
        {
            PrescriptionStatus.NeedMoreInfo => "needMoreInfo",
            PrescriptionStatus.InProduction => "inProduction",
            _ => ToCamelCase(prescriptionStatus.ToString())
        };
    }

    private static string? NormalizeEnumToken(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        return input
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static bool TryParseNumericEnum<TEnum>(string normalized, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;

        if (!byte.TryParse(normalized, out var numericValue) || !Enum.IsDefined(typeof(TEnum), numericValue))
        {
            return false;
        }

        value = (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        return true;
    }

    private static bool SetValue<TEnum>(TEnum input, out TEnum output)
        where TEnum : struct, Enum
    {
        output = input;
        return true;
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length == 1
            ? value.ToLowerInvariant()
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
