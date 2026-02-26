using System.Net;
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
        catch (UnauthorizedAccessException ex)
        {
            await Handle(context, ex, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await Handle(context, ex, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            await Handle(context, ex, HttpStatusCode.InternalServerError, ex.Message);
        }
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