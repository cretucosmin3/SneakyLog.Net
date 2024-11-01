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
        await Task.Delay(1);
    }

    public async Task B3Call2()
    {
        // throw new System.Exception("Bob");
    }
}