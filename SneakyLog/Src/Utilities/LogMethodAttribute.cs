using MethodBoundaryAspect.Fody.Attributes;
using static SneakyLog.SneakyLogContext;

namespace SneakyLog.Utilities;

public class SneakyLogAttribute : OnMethodBoundaryAspect
{
    private MethodTracer? localTrace;

    public override void OnEntry(MethodExecutionArgs args)
    {
        var invocationName = $"{args.Method.DeclaringType?.Name}.{args.Method.Name}";
        localTrace = SneakyLogContext.TraceMethod(invocationName);
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var result = args.ReturnValue != null ? "{...}" : "null";
        localTrace?.SetResult(result);
        // localTrace?.Dispose();
    }

    public override void OnException(MethodExecutionArgs arg)
    {
        var innerException = arg.Exception is AggregateException aggEx ? aggEx.InnerExceptions.FirstOrDefault() ?? arg.Exception : arg.Exception;
        localTrace?.SetException(innerException);
        // localTrace?.Dispose();
    }
}