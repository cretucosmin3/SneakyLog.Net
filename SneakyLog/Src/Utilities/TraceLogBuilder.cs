using System.Diagnostics;
using System.Text;
using SneakyLog.Objects;

namespace SneakyLog.Utilities;

internal class TraceLogBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly Exception? _breakingException;

    public TraceLogBuilder(Exception? breakingException)
    {
        _breakingException = breakingException;
    }

    internal string BuildTraceString(MethodCall call, int depth)
    {
        AddIndentation(depth);

        _builder.Append(call.MethodName);

        LogMethodTime(call);

        bool hasError = call.Exception != null;

        if (hasError)
            LogErrors(call, depth);

        List<MethodCall> children;
        lock (call.Children)
        {
            // TODO: check performance of ordering child calls
            children = call.Children.OrderBy(c => c.StartTime).ToList();
        }

        foreach (var child in children)
        {
            BuildTraceString(child, depth + 1);
        }

        return _builder.ToString();
    }

    private void AddIndentation(int depth)
    {
        if (depth == 0)
        {
            _builder.AppendLine();
            _builder.Append("- ");
        }
        else
        {
            _builder.AppendLine();
            _builder.Append(new string(' ', depth * 2));
            _builder.Append("- ");
        }
    }

    private void LogMethodTime(MethodCall call)
    {
        if (call.EndTime.HasValue)
        {
            var duration = call.EndTime.Value.TotalMilliseconds;
            _builder.Append($" ({duration:F2}ms)");
        }
    }

    private void LogErrors(MethodCall call, int depth)
    {
        bool isLeafNodeWithException = call.Exception != null && call.Children.Count == 0;

        if (isLeafNodeWithException)
        {
            bool isBreakingException = call.Exception == _breakingException;

            if (SneakyLogContext.Config.UseEmojis)
                _builder.Append(isBreakingException ? " ❌" : " ⭕");

            // Do we have multiple exceptions?
            if (call.Exception is AggregateException aggEx && aggEx.InnerExceptions.Count > 1)
                LogMultipleThrownExceptions(aggEx, depth);
            else
                LogOneError(call.Exception);
        }
        else if (call.ReturnValue != null)
        {
            _builder.Append($" => {call.ReturnValue}");
        }
    }

    private void LogMultipleThrownExceptions(AggregateException aggEx, int depth)
    {
        _builder.Append(" Multiple thrown exceptions:");

        foreach (var error in aggEx.InnerExceptions)
        {
            _builder.AppendLine();
            _builder.Append(new string(' ', (depth + 2) * 2));

            if (SneakyLogContext.Config.UseEmojis)
                _builder.Append("⚠️ ");

            _builder.Append($" {GetErrorLineNumber(error)} - {error.GetType().Name}: {error.Message}");
        }
    }

    private void LogOneError(Exception callException)
    {
        string codeSourceWithLine = GetErrorLineNumber(callException);
        string exceptionType = callException.GetType().Name;
        string exceptionMessage = callException.Message;

        _builder.Append($" {codeSourceWithLine} - {exceptionType}: {exceptionMessage}");
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
}