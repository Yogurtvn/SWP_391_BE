namespace ServiceLayer.Exceptions;

public class ApiException : Exception
{
    public ApiException(int statusCode, string errorCode, string message, object? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Details = details;
    }

    public int StatusCode { get; }

    public string ErrorCode { get; }

    public object? Details { get; }
}
