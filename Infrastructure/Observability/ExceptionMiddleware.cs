using System.Net;
using OrderApi.Domain.Exceptions;
using Serilog;

namespace OrderApi.Infrastructure.Observability;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            Log.Error(ex, "Generic handler domain caught exception type={ExceptionType}", ex.GetType().FullName);
            await HandleDomain(context, ex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Generic handler caught exception type={ExceptionType}", ex.GetType().FullName);
            await Handle(context, ex, HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    private static async Task HandleDomain(HttpContext context, DomainException ex)
    {
        Log.Warning(ex, "Domain error {ErrorCode}", ex.ErrorCode);

        var correlationId = context.Items["X-Correlation-Id"]?.ToString();

        var error = new ApiError
        {
            ErrorCode = ex.ErrorCode,
            Message = ex.Message,
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            CorrelationId = correlationId
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(error);
    }

    private static async Task Handle(HttpContext context, Exception ex, HttpStatusCode status, string message)
    {
        Log.Error(ex, "Unhandled exception");

        var correlationId = context.Items["X-Correlation-Id"]?.ToString();

        var error = new ApiError
        {
            Message = message,
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            CorrelationId = correlationId
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(error);
    }
}