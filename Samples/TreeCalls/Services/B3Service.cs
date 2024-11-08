using System;
using System.Threading.Tasks;

public interface IB3Service
{
    Task B3Call1();
    Task B3Call2();
}

public class B3Service : IB3Service
{
    public async Task B3Call1()
    {
        try
        {
            var tasks = new[]
            {
                Task.Run(() => throw new ArgumentException("First error")),
                Task.Run(() => throw new InvalidOperationException("Second error")),
                Task.Run(() => throw new ApplicationException("Third error"))
            };

            Task.WhenAll(tasks).Wait();
        }
        catch (AggregateException ex)
        {
            throw ex;
        }
    }

    public async Task B3Call2()
    {
        await Task.Delay(1);
        string? text = null;
        int x = text.Length;
    }
}