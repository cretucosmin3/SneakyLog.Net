using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using SneakyLog.Objects;

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

        var sb = new StringBuilder();

        lock (calls)
        {
            var rootCalls = calls.OrderBy(c => c.StartTime).ToList();
            if (rootCalls.Count > 0)
            {
                var rootCall = rootCalls[0];
                BuildTraceString(rootCall, sb, 0, breakingException);
                CleanupMethodCallTree(rootCall);
            }
        }

        RequestCalls.TryRemove(requestId, out _);
        Context.Value = null;

        return sb.ToString();
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

    private static void BuildTraceString(MethodCall call, StringBuilder sb, int depth, Exception? breakingException)
    {
        if (depth == 0)
        {
            sb.AppendLine();
            sb.Append("- ");
        }
        else
        {
            sb.AppendLine();
            sb.Append(new string(' ', depth * 2));
            sb.Append("- ");
        }

        sb.Append(call.MethodName);

        if (call.EndTime.HasValue)
        {
            var duration = call.EndTime.Value.TotalMilliseconds;
            sb.Append($" ({duration:F2}ms)");
        }

        // Only show exceptions for leaf nodes (methods that threw the exception)
        if (call.Exception != null && call.Children.Count == 0)
        {
            bool isBreakingException = call.Exception == breakingException;

            if (Config.UseEmojis)
            {
                if (isBreakingException)
                    sb.Append(" ❌");
                else
                    sb.Append(" ⭕");
            }

            if (call.Exception is AggregateException aggEx && aggEx.InnerExceptions.Count > 1)
            {
                sb.Append(" Multiple thrown exceptions:");

                foreach (var error in aggEx.InnerExceptions)
                {
                    sb.AppendLine();
                    sb.Append(new string(' ', (depth + 2) * 2));

                    if(Config.UseEmojis)
                        sb.Append("⚠️ ");

                    sb.Append($" {GetErrorLineNumber(error)} - {error.GetType().Name}: {error.Message}");
                }
            }
            else
            {
                sb.Append($" {GetErrorLineNumber(call.Exception)} - {call.Exception.GetType().Name}: {call.Exception.Message}");
            }
        }
        else if (call.Result != null)
        {
            sb.Append($" => {call.Result}");
        }

        List<MethodCall> children;
        lock (call.Children)
        {
            children = call.Children.OrderBy(c => c.StartTime).ToList();
        }

        foreach (var child in children)
        {
            BuildTraceString(child, sb, depth + 1, breakingException);
        }
    }

    private static string GetErrorLineNumber(Exception ex)
    {
        try
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrames()?.FirstOrDefault();

            if (frame != null)
            {
                var fileName = Path.GetFileName(frame.GetFileName() ?? "Unknown");
                return $"{fileName}:{frame.GetFileLineNumber()}";
            }
        }
        finally
        {
        }

        return "line unknown";
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
}
