using System.Collections.Concurrent;
using System.Text;

namespace SneakyLog;

public class MethodCallInfo
{
    public string MethodName { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; set; }
    public string? Result { get; set; }
    public Exception? Exception { get; set; }
    public int Depth { get; }
    public List<MethodCallInfo> Children { get; } = new();

    public MethodCallInfo(string methodName, int depth)
    {
        MethodName = methodName;
        StartTime = DateTime.UtcNow;
        Depth = depth;
    }
}

public static class ProxyLogContext
{
    private static readonly AsyncLocal<string> RequestId = new();
    private static readonly AsyncLocal<Stack<MethodCallInfo>> CallStack = new();
    private static readonly AsyncLocal<List<MethodCallInfo>> RootCalls = new();
    
    internal static SneakyLogConfig _config { get; private set; } = null!;

    internal static void SetConfig(SneakyLogConfig config)
    {
        _config = config;
    }

    public static void SetRequestIdentifier(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException(nameof(requestId), "Request id cannot be null or empty");
        }

        RequestId.Value = requestId;
        CallStack.Value = new Stack<MethodCallInfo>();
        RootCalls.Value = new List<MethodCallInfo>();
    }

    public static string GetRequestIdentifier() => RequestId.Value ?? "";

    internal static MethodTracer TraceMethod(string methodName)
    {
        var stack = CallStack.Value;
        var depth = stack?.Count ?? 0;
        
        var methodCall = new MethodCallInfo(methodName, depth);
        
        if (stack?.Count > 0)
        {
            stack.Peek().Children.Add(methodCall);
        }
        else
        {
            RootCalls.Value?.Add(methodCall);
        }
        
        stack?.Push(methodCall);
        
        return new MethodTracer(methodCall, () =>
        {
            if (stack?.Count > 0)
            {
                stack.Pop();
            }
        });
    }

    public static string GetTrace()
    {
        var rootCalls = RootCalls.Value;
        if (rootCalls == null || rootCalls.Count == 0)
            return "No trace";

        var sb = new StringBuilder();
        foreach (var call in rootCalls)
        {
            BuildTraceString(call, sb);
        }
        return sb.ToString();
    }

    private static void BuildTraceString(MethodCallInfo call, StringBuilder sb)
    {
        sb.AppendLine();
        sb.Append(new string(' ', call.Depth * 2));
        sb.Append("- ");
        sb.Append(call.MethodName);

        if (call.EndTime.HasValue)
        {
            var duration = call.EndTime.Value - call.StartTime;
            sb.Append($" ({duration.TotalMilliseconds:F2}ms)");
        }

        if (call.Exception != null)
        {
            sb.Append($" ERROR: {call.Exception.GetType().Name}: {call.Exception.Message}");
        }
        else if (call.Result != null)
        {
            sb.Append($" => {call.Result}");
        }

        foreach (var child in call.Children)
        {
            BuildTraceString(child, sb);
        }
    }

    internal class MethodTracer : IDisposable
    {
        private readonly MethodCallInfo _methodCall;
        private readonly Action _onDispose;

        public MethodTracer(MethodCallInfo methodCall, Action onDispose)
        {
            _methodCall = methodCall;
            _onDispose = onDispose;
        }

        public void SetResult(string? result) => _methodCall.Result = result;
        public void SetException(Exception ex) => _methodCall.Exception = ex;

        public void Dispose()
        {
            _methodCall.EndTime = DateTime.UtcNow;
            _onDispose();
        }
    }
}