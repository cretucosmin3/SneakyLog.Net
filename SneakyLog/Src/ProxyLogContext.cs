using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace SneakyLog;

public class MethodCall
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string ParentId { get; }
    public string MethodName { get; }
    public long StartTime { get; }
    public TimeSpan? EndTime { get; private set; }
    public string? Result { get; private set; }
    public Exception? Exception { get; private set; }
    public int ThreadId { get; }
    public List<MethodCall> Children { get; } = [];

    public MethodCall(string methodName, string? parentId = null)
    {
        MethodName = methodName;
        ParentId = parentId ?? "";
        StartTime = Stopwatch.GetTimestamp();
        ThreadId = Environment.CurrentManagedThreadId;
    }

    public void Complete(string? result = null, Exception? exception = null)
    {
        EndTime = Stopwatch.GetElapsedTime(StartTime);
        Result = result;
        Exception = exception;
    }
}

public static class ProxyLogContext
{
    private class AsyncContext
    {
        public string? CurrentMethodId { get; set; }
        public string? RequestId { get; set; }
    }

    // Fix: Properly initialize AsyncLocal with a value factory delegate
    private static readonly AsyncLocal<AsyncContext> Context = new(valueChangedHandler: null);
    private static readonly ConcurrentDictionary<string, MethodCall> ActiveCalls = new();
    private static readonly ConcurrentDictionary<string, List<MethodCall>> RequestCalls = new();

    // Helper property that creates context if it doesn't exist
    private static AsyncContext CurrentContext 
    {
        get
        {
            Context.Value ??= new AsyncContext();
            return Context.Value;
        }
    }

    public static void SetRequestIdentifier(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be null or empty", nameof(requestId));

        CurrentContext.RequestId = requestId;
        RequestCalls.TryAdd(requestId, new List<MethodCall>());
    }

    internal static MethodTracer TraceMethod(string methodName)
    {
        string? parentId = CurrentContext.CurrentMethodId;
        MethodCall methodCall = new(methodName, parentId);

        // Store the call
        ActiveCalls.TryAdd(methodCall.Id, methodCall);

        // If we have a parent, add this as a child
        if (!string.IsNullOrEmpty(parentId) && ActiveCalls.TryGetValue(parentId, out var parentCall))
        {
            lock (parentCall.Children)
            {
                parentCall.Children.Add(methodCall);
            }
        }
        else if (CurrentContext.RequestId != null)
        {
            // This is a root call
            if (RequestCalls.TryGetValue(CurrentContext.RequestId, out var requestCalls))
            {
                lock (requestCalls)
                {
                    requestCalls.Add(methodCall);
                }
            }
        }

        // Set this as the current method
        CurrentContext.CurrentMethodId = methodCall.Id;

        return new MethodTracer(
            methodCall.Id,
            parentId,
            CurrentContext.RequestId,
            () => RestoreContext(parentId));
    }

    private static void RestoreContext(string? parentId)
    {
        CurrentContext.CurrentMethodId = parentId;
    }

    public static string GetTrace()
    {
        var requestId = CurrentContext.RequestId;
        if (requestId == null || !RequestCalls.TryGetValue(requestId, out var calls))
            return "No trace";

        var sb = new StringBuilder();
        lock (calls)
        {
            foreach (var call in calls.OrderBy(c => c.StartTime))
            {
                BuildTraceString(call, sb, 0);
            }
        }

        // Cleanup
        RequestCalls.TryRemove(requestId, out _);

        return sb.ToString();
    }

    public static void EndTrace()
    {
        RequestCalls.TryRemove(CurrentContext.RequestId, out _);
    }

    private static void BuildTraceString(MethodCall call, StringBuilder sb, int depth)
    {
        sb.AppendLine();
        sb.Append(new string(' ', depth * 2));
        sb.Append("- ");
        sb.Append(call.MethodName);

        if (call.EndTime.HasValue)
        {
            var duration = call.EndTime.Value.TotalMilliseconds;
            sb.Append($" ({duration:F2}ms)");
        }

        if (call.Exception != null)
        {
            sb.Append($" ERROR: {call.Exception.GetType().Name}: {call.Exception.Message}");
        }
        else if (call.Result != null)
        {
            sb.Append($" => {call.Result}");
        }

        // Process children (in a thread-safe way)
        var children = call.Children.OrderBy(c => c.StartTime).ToList();
        foreach (var child in children)
        {
            BuildTraceString(child, sb, depth + 1);
        }
    }

    internal class MethodTracer : IDisposable
    {
        private readonly string _methodId;
        private readonly string? _parentId;
        private readonly string? _requestId;
        private readonly Action _onDispose;
        private bool _disposed;

        public MethodTracer(string methodId, string? parentId, string? requestId, Action onDispose)
        {
            _methodId = methodId;
            _parentId = parentId;
            _requestId = requestId;
            _onDispose = onDispose;
        }

        public void SetResult(string? result)
        {
            // Restore context before setting result
            Context.Value = new AsyncContext
            {
                CurrentMethodId = _methodId,
                RequestId = _requestId
            };

            if (ActiveCalls.TryGetValue(_methodId, out var call))
            {
                call.Complete(result);
            }

            RestoreContext(_parentId);
        }

        public void SetException(Exception ex)
        {
            // Restore context before setting exception
            Context.Value = new AsyncContext
            {
                CurrentMethodId = _methodId,
                RequestId = _requestId
            };

            if (ActiveCalls.TryGetValue(_methodId, out var call))
            {
                call.Complete(exception: ex);
            }

            RestoreContext(_parentId);
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Restore context before completing
            Context.Value = new AsyncContext
            {
                CurrentMethodId = _methodId,
                RequestId = _requestId
            };

            if (ActiveCalls.TryGetValue(_methodId, out var call))
            {
                call.Complete();
            }

            _onDispose();
            _disposed = true;
        }
    }
}
