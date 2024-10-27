using System.Diagnostics;
using System.Text.Json;
using Castle.DynamicProxy;
using static SneakyLog.ProxyLogContext;

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
                var result = invocation.ReturnValue != null ? JsonSerializer.Serialize(invocation.ReturnValue) : "null";
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
                    var result = taskResult != null ? JsonSerializer.Serialize(taskResult) : "null";
                    trace.SetResult(result);
                }
            }, TaskScheduler.Current);
        }
    }

    private bool IsAsyncMethod(System.Reflection.MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }

    private string GetCaller(bool isAsync)
    {
        int skipFrames = isAsync ? 4 : 3;
        
        try
        {
            var frame = new StackFrame(skipFrames, false);
            var method = frame.GetMethod();

            // If we hit a proxy or empty method, try the next frame
            if (method?.DeclaringType?.FullName?.Contains("Castle.Proxies") == true)
            {
                frame = new StackFrame(skipFrames + 1, false);
                method = frame.GetMethod();
            }

            if (method == null)
                return "Unknown";

            // Handle async state machine methods
            if (method.DeclaringType?.Name.Contains("d__") == true)
            {
                // Extract the original method name from the async state machine
                string originalMethodName = method.DeclaringType.Name;
                
                // Remove the state machine suffix (like '<SomeMethod>d__2')
                int startIndex = originalMethodName.IndexOf('<') + 1;
                int endIndex = originalMethodName.IndexOf('>');
                if (startIndex > 0 && endIndex > startIndex)
                {
                    originalMethodName = originalMethodName.Substring(startIndex, endIndex - startIndex);
                }

                // Get the containing type (the actual class that declares the async method)
                var containingType = method.DeclaringType.DeclaringType;
                if (containingType != null)
                {
                    return $"{containingType.Name}.{originalMethodName}";
                }
            }

            // For non-async methods or if async parsing fails
            var declaringType = method.DeclaringType;
            if (declaringType == null)
                return method.Name;

            return $"{declaringType.Name}.{method.Name}";
        }
        catch
        {
            return "Unknown";
        }
    }
}