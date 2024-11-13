using System.Diagnostics;

namespace SneakyLog.Objects;

public class MethodCall
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public int ThreadId { get; }
    public string ParentId { get; }

    public string MethodName { get; }
    public long StartTime { get; }
    public TimeSpan? EndTime { get; private set; }

    public List<ParameterInfo> Parameters { get; } = [];
    public object? ReturnValue { get; private set; }
    public Exception? Exception { get; private set; }
    
    public List<MethodCall> Children { get; } = [];

    public MethodCall(string methodName, string? parentId = null)
    {
        MethodName = methodName;
        ParentId = parentId ?? "";
        StartTime = Stopwatch.GetTimestamp();
        ThreadId = Environment.CurrentManagedThreadId;
    }

    public void Complete(object? returnValue = null, Exception? exception = null)
    {
        EndTime = Stopwatch.GetElapsedTime(StartTime);
        ReturnValue = returnValue;
        Exception = exception;
    }
}