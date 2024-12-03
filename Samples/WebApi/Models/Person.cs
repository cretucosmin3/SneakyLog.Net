using SneakyLog.Attributes;

[TraceableObject]
public class Person
{
    [Traceable]
    public int Id { get; set; }
    public string Name { get; set; }
    [Traceable]
    public int Age { get; set; }
    public float Height { get; set; }

    public Preferences? Preferences;
    public Hat[] Hats { get; set; }
    public Car[] Cars { get; set; }
}