using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace SneakyLog;

public class SneakyTracker
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SneakyTracker> _logger;
    private readonly bool _isDebugLoggingEnabled;

    public SneakyTracker(RequestDelegate next, ILogger<SneakyTracker> logger)
    {
        _next = next;
        _logger = logger;
        _isDebugLoggingEnabled = logger.IsEnabled(LogLevel.Debug);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers.TryGetValue("X-Request-Id", out var correlationIds);
        string correlationId = correlationIds.FirstOrDefault() ?? Guid.NewGuid().ToString();
        SneakyLogContext.SetRequestIdentifier(correlationId);

        var endpoint = context.GetEndpoint();
        var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        
        string methodName = "Unknown Endpoint";
        if (controllerActionDescriptor != null)
        {
            methodName = $"{controllerActionDescriptor.DisplayName}";
        }

        using var trace = SneakyLogContext.TraceMethod(methodName);
        
        try
        {
            await _next.Invoke(context);

            if (_isDebugLoggingEnabled)
            {
                string traceOutput = SneakyLogContext.GetTrace();
                _logger.LogDebug("Request completed with trace: {traceOutput}", traceOutput);
            }
        }
        catch (Exception ex)
        {
            string traceOutput = SneakyLogContext.GetTrace(ex.InnerException);
            _logger.LogError("Request errored with trace: {TraceOutput}", traceOutput);
            throw;
        }
        finally
        {
            SneakyLogContext.EndTrace();
        }
    }
}