using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SneakyLog.Objects;

namespace SneakyLog.Utilities;

internal class TraceLogBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly Exception? _breakingException;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        WriteIndented = false
    };

    public TraceLogBuilder(Exception? breakingException)
    {
        _breakingException = breakingException;
    }

    internal string BuildTraceString(MethodCall call, int depth)
    {
        AddIndentation(depth);
        _builder.Append(call.MethodName);
        LogMethodTime(call);

        bool hadErrors = call.Exception != null;

        if (hadErrors)
            LogErrors(call, depth);
        else
            LogMethodData(call, depth);

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

    private void LogMethodData(MethodCall call, int depth)
    {
        if (!SneakyLogContext.Config.DataTraceEnabled)
            return;

        var parameters = call.Parameters;
        var returnValue = call.ReturnValue;

        // Case 1: No parameters and no return
        if (!parameters.Any() && call.HasReturn)
        {
            if (returnValue != null)
                _builder.Append(" >> ").Append(TraceObjectSerializer.SerializeObject(returnValue));
            return;
        }

        // Case 2: Single primitive parameter
        if (parameters.Count == 1 && IsPrimitive(parameters[0].ParameterType))
        {
            _builder.Append($" ({TraceObjectSerializer.SerializeObject(parameters[0].Value)})");
            if (returnValue != null)
                _builder.Append($" >> {TraceObjectSerializer.SerializeObject(returnValue)}");
            return;
        }

        // Case 3: Parameters with objects or multiple parameters
        _builder.AppendLine();
        AddIndentation(depth + 1);
        _builder.Append(">> ");

        var paramStrings = new List<string>();
        foreach (var param in parameters)
        {
            string paramString;
            if (param.IsOut)
                paramString = $"[out {param.Name}]: {TraceObjectSerializer.SerializeObject(param.Value)}";
            else
                paramString = $"{param.Name}: {TraceObjectSerializer.SerializeObject(param.Value)}";

            paramStrings.Add(paramString);
        }

        _builder.Append(string.Join(", ", paramStrings));

        // Return value on new line if it's an object
        if (returnValue != null)
        {
            if (IsPrimitive(returnValue.GetType()))
                _builder.Append($" >> {TraceObjectSerializer.SerializeObject(returnValue)}");
            else
            {
                _builder.AppendLine();
                AddIndentation(depth + 1);
                _builder.Append("<< ").Append(TraceObjectSerializer.SerializeObject(returnValue));
            }
        }
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

            if (call.Exception is AggregateException aggEx && aggEx.InnerExceptions.Count > 1)
                LogMultipleThrownExceptions(aggEx, depth);
            else
                LogOneError(call.Exception);
        }

        // Even with errors, we might want to log the parameters
        LogMethodData(call, depth);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            string str => str,
            DateTime dt => dt.ToString("O"),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            object obj when !IsPrimitive(obj.GetType()) => "{...}",
            _ => value.ToString() ?? "null"
        };
    }

    private static bool IsPrimitive(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) 
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) 
            || type == typeof(Guid) || type.IsEnum;
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
        catch
        {
            // Ignore any errors in getting stack trace information
        }

        return "line unknown";
    }
}