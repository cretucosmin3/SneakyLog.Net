using System.Threading.Tasks;

public class DummyResponse
{
    public int First { get; set; }
    public string Second { get; set; }
}

public interface IAService
{
    public Task<DummyResponse> ACall1();
    public Task ACall2();
}

public class AService : IAService
{
    private readonly IA1Service _a1Service;

    public AService(IA1Service a1Service)
    {
        _a1Service = a1Service;
    }

    public async Task<DummyResponse> ACall1()
    {
        // await Task.Delay(5);

        return new DummyResponse()
        {
            First = _a1Service.A1Call1(),
            Second = _a1Service.A1Call2()
        };
    }

    public async Task ACall2()
    {
        // await Task.Delay(5);

        _a1Service.A1Call1();
    }
}