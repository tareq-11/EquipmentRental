namespace Api.Middleware;

/// <summary>Adds a trusted correlation identifier to the request context, response, and structured log scope.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Processes a request with a bounded caller-provided or generated correlation identifier.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= 128 ? supplied : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })) await next(context);
    }
}
