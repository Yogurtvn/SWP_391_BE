namespace ServiceLayer.Configuration;

public class MomoApiOptions
{
    public string ApiUrl { get; set; } = string.Empty;

    public string PartnerCode { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string RedirectUrl { get; set; } = string.Empty;

    public string IpnUrl { get; set; } = string.Empty;
}
