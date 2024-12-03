namespace SneakyLog.Objects;

internal class MethodTracer : IDisposable
{
    private readonly string _methodId;
    private readonly string? _parentId;
    private readonly string? _requestId;
    private readonly Action _onDispose;
    private bool _disposed;
    private readonly object _lock = new object();

    public MethodTracer(string methodId, string? parentId, string? requestId, Action onDispose)
    {
        _methodId = methodId;
        _parentId = parentId;
        _requestId = requestId;
        _onDispose = onDispose;
    }

    private void RestoreMethodContext(Action action)
    {
        lock (_lock)
        {
            try
            {
                SneakyLogContext.Context.Value = new AsyncContext
                {
                    CurrentMethodId = _methodId,
                    RequestId = _requestId
                };

                action();
            }
            finally
            {
                SneakyLogContext.RestoreContext(_parentId);
            }
        }
    }

    public void SetParameters(List<ParameterInfo> parameters)
    {
        if (SneakyLogContext.ActiveCalls.TryGetValue(_methodId, out var call))
        {
            call.Parameters.AddRange(parameters);
        }
    }

    public void SetResult(object? result)
    {
        RestoreMethodContext(() =>
        {
            if (SneakyLogContext.ActiveCalls.TryGetValue(_methodId, out var call))
            {
                call.Complete(result);
            }
        });
    }

    public void SetException(Exception ex)
    {
        RestoreMethodContext(() =>
        {
            if (SneakyLogContext.ActiveCalls.TryGetValue(_methodId, out var call))
            {
                call.Complete(exception: ex);
            }
        });
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            SneakyLogContext.Context.Value = new AsyncContext
            {
                CurrentMethodId = _methodId,
                RequestId = _requestId
            };

            if (SneakyLogContext.ActiveCalls.TryGetValue(_methodId, out var call))
            {
                if (!call.EndTime.HasValue)
                {
                    call.Complete();
                }
            }

            _onDispose();
            _disposed = true;
        }
    }
}