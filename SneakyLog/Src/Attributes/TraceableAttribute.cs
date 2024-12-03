namespace SneakyLog.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class TraceableAttribute : Attribute 
{
    public bool MaskValue { get; }
    
    public TraceableAttribute(bool maskValue = false)
    {
        MaskValue = maskValue;
    }
}