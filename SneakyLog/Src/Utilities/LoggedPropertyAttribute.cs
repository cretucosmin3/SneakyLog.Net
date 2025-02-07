[AttributeUsage(AttributeTargets.Property)]
public class LoggedPropertyAttribute : Attribute
{
    public string Name { get; set; }
    public bool MaskValue { get; set; } = false;
    public string MaskPattern { get; set; } = "****";
}