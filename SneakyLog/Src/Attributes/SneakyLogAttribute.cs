namespace SneakyLog.Attributes;

public enum TraceLevel
{
    /// <summary>
    /// This is the default behaviour if config `DataTraceEnabled` is set to true.
    /// </summary>
    AllowedParamsAndReturn,
    ParamsAndReturn,
    AllowedParams,
    Params,
    Return
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TraceWithDataAttribute : Attribute
{
    public TraceLevel DataTraceLevel { get; }
    
    public TraceWithDataAttribute(TraceLevel dataTraceLevel)
    {
        DataTraceLevel = dataTraceLevel;
    }
}
