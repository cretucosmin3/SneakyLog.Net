using System.Threading.Tasks;

public interface IBService
{
    public void BCall1();
    public Task BCall2();
}

public class BService : IBService
{
    private readonly IB1Service _b1Service;

    public BService(IB1Service b1Service)
    {
        _b1Service = b1Service;
    }

    public void BCall1()
    {
        _b1Service.B1Call1();
        _b1Service.B1Call2();
    }

    public async Task BCall2()
    {
        // await Task.Delay(1);

        _b1Service.B1Call2();
    }
}