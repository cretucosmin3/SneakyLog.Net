using System;
using System.Threading.Tasks;

public interface IB3Service
{
    public Task B3Call1();
    public Task B3Call2();
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

            // Use Wait() instead of await to preserve the AggregateException
            Task.WhenAll(tasks).Wait();
        }
        catch (AggregateException ex)
        {
            // Re-throw the AggregateException directly without it being unwrapped
            throw ex;
        }

        // await Task.Delay(1);
    }

    public async Task B3Call2()
    {
        throw new System.Exception("Just testing...");
        // throw new System.Exception("Just testing...");
    }
}