namespace LMS.API.Middleware;
using LMS.Application.Common.Modals;
using Microsoft.AspNetCore.Http;
public class ExceptionMiddleware
{
  private readonly RequestDelegate _next;

  public ExceptionMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      await HandleExceptionAsync(context, ex);
    }
  }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
  {
    context.Response.ContentType = "application/json";
    
    int statusCode = ex switch
    {
      AppException appEx1 => appEx1.StatusCode,
      _ => StatusCodes.Status500InternalServerError
    };
    context.Response.StatusCode = statusCode;

    string message = ex.Message;
    string errorCode = ex switch {
       AppException appEx2 => appEx2.ErrorCode,
       _ => "INTERNAL_SERVER_ERROR"
    };

    var errorResponse = new ErrorResponse(errorCode, message); 
    
    await context.Response.WriteAsJsonAsync(errorResponse);
  }
}