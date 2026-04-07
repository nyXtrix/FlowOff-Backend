namespace LMS.Application.Common.Modals;

public class AppException(int statusCode, string message, string errorCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}

public class ErrorResponse(string v1, string v2)
{
    private readonly string v1 = v1;
    private readonly string v2 = v2;

    public string? ErrorCode { get; set; }
    public object? Message { get; set; }
}

public class ValidationErrorResponse
{
    public string? ErrorCode { get; set; }
    public string Field { get; set; } = string.Empty;
    public object? Message { get; set; }
}