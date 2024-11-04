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

            // HandleThrownException(ex, trace);
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
                    HandleThrownException(t.Exception, trace);
                }
                else if (t.GetType().IsGenericType)
                {
                    HandleTaskResult(t, trace);
                }
            }, TaskScheduler.Current);
        }
    }

    private void HandleThrownException(Exception? exception, SneakyLogContext.MethodTracer trace)
    {
        if (exception == null)
        {
            trace.SetException(new Exception("Unknown async error"));
            return;
        }

        if (exception is AggregateException aggEx)
        {
            // TODO: trace multiple exceptions thrown at once
            // foreach (var innerEx in aggEx.InnerExceptions)
            // {
            //     trace.AddError(innerEx);
            // }

            trace.SetException(aggEx.InnerExceptions.First());
        }
        else
        {
            trace.SetException(exception);
        }
    }

    private void HandleTaskResult(Task task, SneakyLogContext.MethodTracer trace)
    {
        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            trace.SetResult(resultProperty?.GetValue(task) != null ? "{...}" : "null");
        }
        catch (Exception ex)
        {
            HandleThrownException(ex.InnerException ?? ex, trace);
        }
    }

    private bool IsAsyncMethod(System.Reflection.MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }
}