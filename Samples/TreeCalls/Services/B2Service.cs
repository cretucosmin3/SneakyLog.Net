using System.Threading.Tasks;

public interface IB2Service
{
    public Task B2Call1();
    public Task B2Call2();
}

public class B2Service : IB2Service
{
    public async Task B2Call1()
    {
        // await Task.Delay(1);
    }

    public async Task B2Call2()
    {
        // await Task.Delay(1);
    }
}