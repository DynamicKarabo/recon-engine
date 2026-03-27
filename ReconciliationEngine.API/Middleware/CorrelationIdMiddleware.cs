using System.Diagnostics;

namespace ReconciliationEngine.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        await _next(context);
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var existingCorrelationId) 
            && !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
