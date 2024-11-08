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
                    var exception = t.Exception?.InnerExceptions.FirstOrDefault() ?? t.Exception;
                    trace.SetException(exception ?? new Exception("Unknown async error"));
                }
                else if (t.GetType().IsGenericType)
                {
                    HandleTaskResult(t, trace);
                }
                else
                {
                    trace.SetResult("void");
                }
            }, TaskScheduler.Current);
        }
    }

    private void HandleTaskResult(Task task, SneakyLogContext.MethodTracer trace)
    {
        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task);
            trace.SetResult(result != null ? "{...}" : "null");
        }
        catch (Exception ex)
        {
            trace.SetException(ex.InnerException ?? ex);
        }
    }

    private bool IsAsyncMethod(System.Reflection.MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }
}