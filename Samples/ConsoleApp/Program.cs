using System;
using SneakyLog;
using SneakyLog.Utilities;

public static class Program
{
    public static void Main(string[] args)
    {
        SneakyLogContext.SetRequestIdentifier("Just Testing");

        try
        {
            Level1();
        }
        catch (System.Exception ex)
        {
            string trace = SneakyLogContext.GetTrace(ex);
            Console.WriteLine(trace);
        }
    }

    [SneakyLog]
    public static void Level1()
    {
        Console.WriteLine("Bob");

        Level2();
    }

    public static void Level2()
    {
        throw new System.Exception("Testing");
    }
}