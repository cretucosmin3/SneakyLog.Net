public static class TestingData
{
    public static Person[] People = [
        new() {
            Id = 1,
            Name = "Bob Marley",
            Age = 44,
            Height = 183,
        }
    ];

    public static Hat[] Hats = [
        new() {
           PersonId = 1,
           Color = "Blue",
           Pattern = "Army",
           Price = 50
        },
        new() {
           PersonId = 1,
           Color = "Green",
           Pattern = "Plain",
           Price = 40
        },
    ];

    public static Car[] Cars = [
        new() {
            PersonId = 1,
            Make = "Audi",
            Price = 14_000
        },
    ];
}