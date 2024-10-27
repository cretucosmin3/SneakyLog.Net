namespace SneakyLog.Utilities;

public interface ILogTraceStore
{
    void StartTrace();
    void AddMethodCall(string methodName);
    void AddResult(string methodName, string result);
    void DeleteTrace();
}

public class LogTraceStore : ILogTraceStore
{
    public void StartTrace() => Console.WriteLine("Trace started");
    public void AddMethodCall(string methodName) => Console.WriteLine($"Method called: {methodName}");
    public void AddResult(string methodName, string result) => Console.WriteLine($"Method {methodName} returned: {result}");
    public void DeleteTrace() => Console.WriteLine("Trace deleted");
}