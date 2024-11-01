using Castle.DynamicProxy;

namespace SneakyLog.Utilities;

public class SneakyInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
        using var trace = ProxyLogContext.TraceMethod(methodName);

        try
        {
            invocation.Proceed();

            if (IsAsyncMethod(invocation.Method))
            {
                HandleAsyncMethod(invocation, trace);
            }
            else
            {
                // var result = invocation.ReturnValue != null ? JsonSerializer.Serialize(invocation.ReturnValue) : "null";
                var result = invocation.ReturnValue != null ? "{...}" : "null";
                trace.SetResult(result);
            }
        }
        catch (Exception ex)
        {
            trace.SetException(ex);
            throw;
        }
    }

    private void HandleAsyncMethod(IInvocation invocation, ProxyLogContext.MethodTracer trace)
    {
        if (invocation.ReturnValue is Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var exception = t.Exception?.InnerException ?? t.Exception;
                    trace.SetException(exception ?? new Exception("Unknown async error"));
                }
                else if (t.GetType().IsGenericType)
                {
                    var resultProperty = t.GetType().GetProperty("Result");
                    var taskResult = resultProperty?.GetValue(t);
                    // var result = taskResult != null ? JsonSerializer.Serialize(taskResult) : "null";
                    var result = invocation.ReturnValue != null ? "{...}" : "null";
                    trace.SetResult(result);
                }
            }, TaskScheduler.Current);
        }
    }

    private bool IsAsyncMethod(System.Reflection.MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }
}