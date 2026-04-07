namespace LMS.Application.Common.Modals;

public class AppException(int statusCode, string message, string errorCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}

public class ErrorResponse(string errorCode, object message)
{
    public string ErrorCode { get; set; } = errorCode;
    public object Message { get; set; } = message;
}

public class ValidationErrorResponse
{
    public string? ErrorCode { get; set; }
    public string Field { get; set; } = string.Empty;
    public object? Message { get; set; }
}