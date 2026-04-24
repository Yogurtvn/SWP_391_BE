using System.Globalization;
using System.Net;
using System.Text;

namespace ServiceLayer.Services.Notifications;

internal sealed class PreOrderBackInStockEmailTemplateData
{
    public string? CustomerName { get; init; }

    public int? OrderId { get; init; }

    public string? ProductName { get; init; }

    public string? Sku { get; init; }

    public string? VariantInfo { get; init; }

    public DateTime? ExpectedRestockDate { get; init; }

    public DateTime UpdatedAt { get; init; }

    public string? OrderTrackingUrl { get; init; }
}

internal static class PreOrderBackInStockEmailTemplateBuilder
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string Build(PreOrderBackInStockEmailTemplateData templateData)
    {
        ArgumentNullException.ThrowIfNull(templateData);

        var greetingName = HtmlEncode(NormalizeValue(templateData.CustomerName) ?? "Qu\u00FD kh\u00E1ch");
        var infoRows = BuildInfoRows(templateData);
        var callToAction = BuildCallToAction(templateData.OrderTrackingUrl);

        return $"""
<!DOCTYPE html>
<html lang="vi">
<head>
  <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>[E-World] S&#7843;n ph&#7849;m b&#7841;n &#273;&#7863;t tr&#432;&#7899;c &#273;&#227; v&#7873; h&#224;ng</title>
</head>
<body style="margin:0;padding:0;background-color:#f8f2e6;">
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color:#f8f2e6;margin:0;padding:0;">
    <tr>
      <td align="center" style="padding:24px 12px;">
        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width:620px;background-color:#fffdf9;border:1px solid #e0c79d;border-radius:14px;">
          <tr>
            <td style="padding:20px 28px;background-color:#f1dfbd;border-bottom:1px solid #e0c79d;">
              <p style="margin:0;font-family:Segoe UI,Arial,sans-serif;font-size:26px;line-height:32px;font-weight:700;color:#6c4525;">E-World</p>
            </td>
          </tr>
          <tr>
            <td style="padding:28px;">
              <p style="margin:0 0 14px 0;font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:24px;color:#5b4634;">Xin ch&#224;o {greetingName},</p>
              <h1 style="margin:0 0 14px 0;font-family:Segoe UI,Arial,sans-serif;font-size:28px;line-height:36px;font-weight:700;color:#5f3f22;">S&#7843;n ph&#7849;m b&#7841;n &#273;&#7863;t tr&#432;&#7899;c &#273;&#227; v&#7873; h&#224;ng</h1>
              <p style="margin:0 0 20px 0;font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:24px;color:#5b4634;">
                E-World xin th&#244;ng b&#225;o s&#7843;n ph&#7849;m b&#7841;n &#273;&#7863;t tr&#432;&#7899;c hi&#7879;n &#273;&#227; v&#7873; h&#224;ng. B&#7841;n c&#243; th&#7875; truy c&#7853;p h&#7879; th&#7889;ng &#273;&#7875; ti&#7871;p t&#7909;c theo d&#245;i v&#224; nh&#7853;n &#273;&#417;n h&#224;ng c&#7911;a m&#236;nh.
              </p>
              <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border:1px solid #eadcc4;background-color:#fff9ef;border-radius:10px;">
                <tr>
                  <td style="padding:14px 16px;border-bottom:1px solid #eadcc4;">
                    <p style="margin:0;font-family:Segoe UI,Arial,sans-serif;font-size:14px;line-height:22px;font-weight:700;color:#6c4525;">Th&#244;ng tin &#273;&#417;n pre-order</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:10px 16px 14px 16px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                      {infoRows}
                    </table>
                  </td>
                </tr>
              </table>
              <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-top:22px;">
                <tr>
                  <td align="center">
                    {callToAction}
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style="padding:18px 28px;background-color:#fcf4e5;border-top:1px solid #eadcc4;">
              <p style="margin:0;font-family:Segoe UI,Arial,sans-serif;font-size:13px;line-height:20px;color:#7c684f;">
                C&#7843;m &#417;n b&#7841;n &#273;&#227; &#273;&#7891;ng h&#224;nh c&#249;ng E-World.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }

    private static string BuildInfoRows(PreOrderBackInStockEmailTemplateData templateData)
    {
        var rowsBuilder = new StringBuilder();

        if (templateData.OrderId.HasValue)
        {
            AppendInfoRow(rowsBuilder, "M\u00E3 \u0111\u01A1n h\u00E0ng", $"#{templateData.OrderId.Value}");
        }

        var productName = NormalizeValue(templateData.ProductName);
        if (!string.IsNullOrWhiteSpace(productName))
        {
            AppendInfoRow(rowsBuilder, "S\u1EA3n ph\u1EA9m", productName);
        }

        var skuOrVariant = BuildSkuOrVariant(templateData.Sku, templateData.VariantInfo);
        if (!string.IsNullOrWhiteSpace(skuOrVariant))
        {
            AppendInfoRow(rowsBuilder, "SKU / phi\u00EAn b\u1EA3n", skuOrVariant);
        }

        if (templateData.ExpectedRestockDate.HasValue)
        {
            AppendInfoRow(
                rowsBuilder,
                "Ng\u00E0y d\u1EF1 ki\u1EBFn v\u1EC1 h\u00E0ng",
                FormatDateTime(templateData.ExpectedRestockDate.Value));
        }

        AppendInfoRow(rowsBuilder, "Th\u1EDDi \u0111i\u1EC3m c\u1EADp nh\u1EADt", FormatDateTime(templateData.UpdatedAt));

        return rowsBuilder.ToString();
    }

    private static string BuildSkuOrVariant(string? sku, string? variantInfo)
    {
        var normalizedSku = NormalizeValue(sku);
        var normalizedVariantInfo = NormalizeValue(variantInfo);

        if (!string.IsNullOrWhiteSpace(normalizedSku) && !string.IsNullOrWhiteSpace(normalizedVariantInfo))
        {
            return $"{normalizedSku} | {normalizedVariantInfo}";
        }

        return normalizedSku ?? normalizedVariantInfo ?? string.Empty;
    }

    private static void AppendInfoRow(StringBuilder rowsBuilder, string label, string value)
    {
        rowsBuilder.Append(
            $"""
<tr>
  <td valign="top" style="width:34%;padding:7px 8px 7px 0;font-family:Segoe UI,Arial,sans-serif;font-size:13px;line-height:20px;color:#8a6f53;">{HtmlEncode(label)}</td>
  <td valign="top" style="padding:7px 0;font-family:Segoe UI,Arial,sans-serif;font-size:14px;line-height:20px;font-weight:600;color:#4f3a27;">{HtmlEncode(value)}</td>
</tr>
""");
    }

    private static string BuildCallToAction(string? orderTrackingUrl)
    {
        var normalizedUrl = NormalizeValue(orderTrackingUrl);
        if (string.IsNullOrWhiteSpace(normalizedUrl)
            || !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return """
<span style="display:inline-block;padding:13px 24px;border-radius:8px;background-color:#9b6f3d;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:14px;font-weight:700;text-decoration:none;">
  Ti&#7871;p t&#7909;c theo d&#245;i &#273;&#417;n h&#224;ng
</span>
""";
        }

        return $"""
<a href="{HtmlEncode(normalizedUrl)}" target="_blank" rel="noopener" style="display:inline-block;padding:13px 24px;border-radius:8px;background-color:#9b6f3d;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:14px;font-weight:700;text-decoration:none;">
  Ti&#7871;p t&#7909;c theo d&#245;i &#273;&#417;n h&#224;ng
</a>
""";
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy HH:mm", VietnameseCulture);
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string HtmlEncode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
