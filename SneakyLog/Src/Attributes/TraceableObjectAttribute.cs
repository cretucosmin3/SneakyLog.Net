namespace SneakyLog.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TraceableObjectAttribute : Attribute
{
    public TraceStrategy Strategy { get; }
    
    public TraceableObjectAttribute(TraceStrategy strategy = TraceStrategy.OptIn)
    {
        Strategy = strategy;
    }
}

public enum TraceStrategy
{
    /// <summary>
    /// Only properties marked with [Traceable] will be logged
    /// </summary>
    OptIn,
    
    /// <summary>
    /// All properties except those marked with [NotTraceable] will be logged
    /// </summary>
    OptOut
}