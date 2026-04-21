namespace ServiceLayer.Configuration;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Online Eyewear";

    public bool EnableSsl { get; set; } = true;
}
