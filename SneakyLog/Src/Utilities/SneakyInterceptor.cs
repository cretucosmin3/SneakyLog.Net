using System.Reflection;
using Castle.DynamicProxy;
using SneakyLog.Attributes;
using SneakyLog.Objects;

namespace SneakyLog.Utilities;

public class SneakyInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var methodInfo = invocation.Method;
        var methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
        
        // Check for trace attributes
        var noTraceAttr = methodInfo.GetCustomAttribute<NoDataTraceAttribute>();
        var traceWithDataAttr = methodInfo.GetCustomAttribute<TraceWithDataAttribute>();
        
        bool shouldTraceParams = ShouldTraceParameters(traceWithDataAttr, noTraceAttr);
        bool shouldTraceReturn = ShouldTraceReturn(traceWithDataAttr, noTraceAttr);
        
        using var trace = SneakyLogContext.TraceMethod(
            methodName, 
            hasParams: shouldTraceParams && invocation.Arguments.Length > 0,
            isVoid: shouldTraceReturn && methodInfo.ReturnType != typeof(void)
        );

        if (shouldTraceParams)
        {
            CaptureParameters(invocation, trace);
        }

        try
        {
            invocation.Proceed();

            if (IsAsyncMethod(methodInfo))
            {
                HandleAsyncMethod(invocation, trace, shouldTraceReturn);
            }
            else if (shouldTraceReturn)
            {
                trace.SetResult(invocation.ReturnValue);
            }
        }
        catch (Exception ex)
        {
            var innerException = ex is AggregateException aggEx ? 
                aggEx.InnerExceptions.FirstOrDefault() ?? ex : ex;
            trace.SetException(innerException);
            throw;
        }
    }

    private bool ShouldTraceParameters(TraceWithDataAttribute? traceAttr, NoDataTraceAttribute? noTraceAttr)
    {
        if (noTraceAttr != null) return false;
        if (!SneakyLogContext.Config.DataTraceEnabled) return false;
        
        return traceAttr?.DataTraceLevel switch
        {
            TraceLevel.AllowedParamsAndReturn => true,
            TraceLevel.ParamsAndReturn => true,
            TraceLevel.AllowedParams => true,
            TraceLevel.Params => true,
            TraceLevel.Return => false,
            _ => SneakyLogContext.Config.DataTraceEnabled
        };
    }

    private bool ShouldTraceReturn(TraceWithDataAttribute? traceAttr, NoDataTraceAttribute? noTraceAttr)
    {
        if (noTraceAttr != null) return false;
        if (!SneakyLogContext.Config.DataTraceEnabled) return false;

        return traceAttr?.DataTraceLevel switch
        {
            TraceLevel.AllowedParamsAndReturn => true,
            TraceLevel.ParamsAndReturn => true,
            TraceLevel.Return => true,
            _ => SneakyLogContext.Config.DataTraceEnabled
        };
    }

    private void HandleAsyncMethod(IInvocation invocation, MethodTracer trace, bool shouldTraceReturn)
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
                else if (shouldTraceReturn)
                {
                    HandleTaskResult(t, trace);
                }
            }, TaskScheduler.Current);
        }
    }

    private void CaptureParameters(IInvocation invocation, MethodTracer trace)
    {
        var parameters = invocation.Method.GetParameters();
        var parameterInfos = new List<Objects.ParameterInfo>();

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            parameterInfos.Add(new Objects.ParameterInfo
            {
                Name = param.Name,
                Value = invocation.Arguments[i],
                IsOut = param.IsOut,
                IsIn = param.IsIn,
                ParameterType = param.ParameterType
            });
        }

        trace.SetParameters(parameterInfos);
    }

    private void HandleTaskResult(Task task, MethodTracer trace)
    {
        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task);
            trace.SetResult(result);
        }
        catch (Exception ex)
        {
            trace.SetException(ex.InnerException ?? ex);
        }
    }

    private bool IsAsyncMethod(MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }
}