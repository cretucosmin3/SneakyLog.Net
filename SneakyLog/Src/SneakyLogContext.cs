using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using SneakyLog.Objects;
using SneakyLog.Utilities;

namespace SneakyLog;

public static class SneakyLogContext
{
    internal static SneakyLogConfig Config { get; private set; } = new();

    internal static readonly AsyncLocal<AsyncContext> Context = new(valueChangedHandler: null);
    internal static readonly ConcurrentDictionary<string, MethodCall> ActiveCalls = new();
    private static readonly ConcurrentDictionary<string, List<MethodCall>> RequestCalls = new();

    private static AsyncContext CurrentContext
    {
        get
        {
            Context.Value ??= new AsyncContext();
            return Context.Value;
        }
    }

    internal static void SetConfig(SneakyLogConfig config)
    {
        Config = config;
    }

    public static void SetRequestIdentifier(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be null or empty", nameof(requestId));

        // Clean up existing request if present
        if (CurrentContext.RequestId != null && CurrentContext.RequestId != requestId)
        {
            EndTrace();
        }

        CurrentContext.RequestId = requestId;
        RequestCalls.TryAdd(requestId, new List<MethodCall>());
    }

    public static string GetTrace(Exception? breakingException = null)
    {
        var requestId = CurrentContext.RequestId;

        if (requestId == null || !RequestCalls.TryGetValue(requestId, out var calls))
            return "No trace";

        string logText = "No trace";

        lock (calls)
        {
            var rootCalls = calls.OrderBy(c => c.StartTime).ToList();
            if (rootCalls.Count > 0)
            {
                var rootCall = rootCalls[0];

                TraceLogBuilder logBuilder = new(breakingException);

                logText = logBuilder.BuildTraceString(rootCall, 0);

                CleanupMethodCallTree(rootCall);
            }
        }

        RequestCalls.TryRemove(requestId, out _);
        Context.Value = null;

        return logText;
    }

    public static void EndTrace()
    {
        var requestId = CurrentContext.RequestId;

        if (requestId != null)
        {
            if (RequestCalls.TryGetValue(requestId, out var calls))
            {
                lock (calls)
                {
                    foreach (var call in calls)
                    {
                        CleanupMethodCallTree(call);
                    }
                }
            }

            RequestCalls.TryRemove(requestId, out _);
        }

        // Clear the context
        Context.Value = null;
    }

    internal static void RestoreContext(string? parentId)
    {
        CurrentContext.CurrentMethodId = parentId;
    }

    private static void CleanupMethodCallTree(MethodCall call)
    {
        List<MethodCall> childrenCopy;

        lock (call.Children)
        {
            childrenCopy = new List<MethodCall>(call.Children);
        }

        foreach (var child in childrenCopy)
        {
            CleanupMethodCallTree(child);
        }

        // Remove this call from ActiveCalls
        ActiveCalls.TryRemove(call.Id, out _);
    }

    internal static MethodTracer TraceMethod(string methodName, bool hasParams = false, bool isVoid = false)
    {
        string? parentId = CurrentContext.CurrentMethodId;
        MethodCall methodCall = new(methodName, hasParams, isVoid, parentId);

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
}
