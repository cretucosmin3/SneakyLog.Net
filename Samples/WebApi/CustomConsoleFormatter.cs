using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

public class CustomConsoleFormatter : ConsoleFormatter
{
    public CustomConsoleFormatter() : base("CustomConsole") { }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        // if (!IsEnabled(logEntry.LogLevel)) return;

        var logLevel = logEntry.LogLevel.ToString().ToUpper()[0];
        var category = logEntry.Category.Split('.').Last();
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        textWriter.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{logLevel}] {category}: {message}");
    }
}