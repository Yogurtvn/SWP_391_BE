namespace ControllerLayer.Models;

public class ApiErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public object? Details { get; set; }
}
