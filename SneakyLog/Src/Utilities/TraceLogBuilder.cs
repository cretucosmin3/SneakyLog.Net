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

        _builder.Append(call.MethodName);

        if (call.EndTime.HasValue)
        {
            var duration = call.EndTime.Value.TotalMilliseconds;
            _builder.Append($" ({duration:F2}ms)");
        }

        // Only show exceptions for leaf nodes (methods that threw the exception)
        if (call.Exception != null && call.Children.Count == 0)
        {
            bool isBreakingException = call.Exception == _breakingException;

            if (SneakyLogContext.Config.UseEmojis)
            {
                if (isBreakingException)
                    _builder.Append(" ❌");
                else
                    _builder.Append(" ⭕");
            }

            if (call.Exception is AggregateException aggEx && aggEx.InnerExceptions.Count > 1)
            {
                _builder.Append(" Multiple thrown exceptions:");

                foreach (var error in aggEx.InnerExceptions)
                {
                    _builder.AppendLine();
                    _builder.Append(new string(' ', (depth + 2) * 2));

                    if(SneakyLogContext.Config.UseEmojis)
                        _builder.Append("⚠️ ");

                    _builder.Append($" {GetErrorLineNumber(error)} - {error.GetType().Name}: {error.Message}");
                }
            }
            else
            {
                _builder.Append($" {GetErrorLineNumber(call.Exception)} - {call.Exception.GetType().Name}: {call.Exception.Message}");
            }
        }
        else if (call.Result != null)
        {
            _builder.Append($" => {call.Result}");
        }

        List<MethodCall> children;
        lock (call.Children)
        {
            children = call.Children.OrderBy(c => c.StartTime).ToList();
        }

        foreach (var child in children)
        {
            BuildTraceString(child, depth + 1);
        }

        return _builder.ToString();
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