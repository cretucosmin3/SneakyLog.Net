namespace SneakyLog.Objects;

public class ParameterInfo
{
    public string? Name { get; set; }
    public object? Value { get; set; }
    public bool IsOut { get; set; }
    public bool IsIn { get; set; }
    public Type ParameterType { get; set; }
}