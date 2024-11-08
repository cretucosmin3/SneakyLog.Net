using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace SneakyLog;

public class SneakyTracker
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SneakyTracker> _logger;
    private readonly bool _isInformationEnabled;

    public SneakyTracker(RequestDelegate next, ILogger<SneakyTracker> logger)
    {
        _next = next;
        _logger = logger;
        _isInformationEnabled = logger.IsEnabled(LogLevel.Information);
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
            methodName = $"{controllerActionDescriptor.ControllerName}Controller.{controllerActionDescriptor.ActionName}";
        }

        using var trace = SneakyLogContext.TraceMethod(methodName);
        
        try
        {
            await _next.Invoke(context);

            if (_isInformationEnabled)
            {
                string traceOutput = SneakyLogContext.GetTrace();
                _logger.LogDebug("Request completed with trace: {traceOutput}", traceOutput);
            }
        }
        catch (Exception ex)
        {
            string traceOutput = SneakyLogContext.GetTrace(ex.InnerException);
            _logger.LogError("Endpoint errored with trace: {TraceOutput}", traceOutput);
            throw;
        }
        finally
        {
            if (!context.RequestAborted.IsCancellationRequested)
            {
                SneakyLogContext.EndTrace();
            }
        }
    }
}