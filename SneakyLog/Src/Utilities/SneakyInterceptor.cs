using Castle.DynamicProxy;

namespace SneakyLog.Utilities;

public class SneakyInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
        using var trace = SneakyLogContext.TraceMethod(methodName);

        try
        {
            invocation.Proceed();

            if (IsAsyncMethod(invocation.Method))
            {
                HandleAsyncMethod(invocation, trace);
            }
            else
            {
                var result = invocation.ReturnValue != null ? "{...}" : "null";
                trace.SetResult(result);
            }
        }
        catch (Exception ex)
        {
            // Unwrap aggregate exceptions
            var innerException = ex is AggregateException aggEx ? aggEx.InnerExceptions.FirstOrDefault() ?? ex : ex;
            trace.SetException(innerException);
            throw;
        }
    }

    private void HandleAsyncMethod(IInvocation invocation, SneakyLogContext.MethodTracer trace)
    {
        if (invocation.ReturnValue is Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var exception = t.Exception?.InnerExceptions.FirstOrDefault()
                        ?? t.Exception
                        ?? new Exception("Unknown async error");
                    trace.SetException(exception);
                }
                else if (t.GetType().IsGenericType)
                {
                    try
                    {
                        var resultProperty = t.GetType().GetProperty("Result");
                        var taskResult = resultProperty?.GetValue(t);
                        var result = taskResult != null ? "{...}" : "null";
                        trace.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions that might occur while getting the Result
                        var actualException = ex.InnerException ?? ex;
                        trace.SetException(actualException);
                    }
                }
            }, TaskScheduler.Current);
        }
    }

    private bool IsAsyncMethod(System.Reflection.MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }
}