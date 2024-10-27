public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public float Height { get; set; }

    public Preferences? Preferences;
    public Hat[] Hats { get; set; }
    public Car[] Cars { get; set; }
}