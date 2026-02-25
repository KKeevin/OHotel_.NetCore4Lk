using System.Net;
using System.Text.Json;
using OHotel.NETCoreMVC.Models;

namespace OHotel.NETCoreMVC.Middleware;

/// <summary>全域例外處理中介軟體，統一捕獲並回傳錯誤回應</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "未處理的例外: {Message}", exception.Message);

        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = statusCode == HttpStatusCode.InternalServerError && !_env.IsDevelopment()
                ? "伺服器發生錯誤，請稍後再試"
                : exception.Message,
            StatusCode = (int)statusCode
        };

        if (_env.IsDevelopment())
            response.Detail = exception.ToString();

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
