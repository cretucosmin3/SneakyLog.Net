using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace SneakyLog;

public class SneakyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SneakyMiddleware> _logger;
    private readonly bool _isInformationEnabled;

    public SneakyMiddleware(RequestDelegate next, ILogger<SneakyMiddleware> logger)
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

        // Get the endpoint for better method naming
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
                _logger.LogInformation($"Request completed with trace: {traceOutput}");
            }
        }
        catch (Exception ex)
        {
            trace.SetException(ex);
            string traceOutput = SneakyLogContext.GetTrace();
            _logger.LogError($"Endpoint errored with trace: {traceOutput}");
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