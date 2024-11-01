using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SneakyLog;

public class SneakyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SneakyMiddleware> _logger;

    public SneakyMiddleware(RequestDelegate next, ILogger<SneakyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers.TryGetValue("X-Request-Id", out var correlationIds);

        string correlationId = correlationIds.FirstOrDefault() ?? Guid.NewGuid().ToString();

        ProxyLogContext.SetRequestIdentifier(correlationId);

        // var endpoint = context.GetEndpoint();

        try
        {
            await _next.Invoke(context);
        }
        catch (Exception)
        {
            string trace = ProxyLogContext.GetTrace();
            _logger.LogError($"Endpoint errored with trace: {trace}");

            throw;
        }

        ProxyLogContext.EndTrace();
    }
}