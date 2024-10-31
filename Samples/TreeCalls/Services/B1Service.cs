using System.Threading.Tasks;

public interface IB1Service
{
    public void B1Call1();
    public void B1Call2();
}

public class B1Service : IB1Service
{
    private readonly IB2Service _b2Service;
    private readonly IB3Service _b3Service;

    public B1Service(IB2Service b2Service, IB3Service b3Service)
    {
        _b2Service = b2Service;
        _b3Service = b3Service;
    }

    public void B1Call1()
    {
        _b2Service.B2Call1().Wait();
        _b2Service.B2Call2().Wait();

        Task.WaitAll(_b3Service.B3Call1(), _b3Service.B3Call2(), _b2Service.B2Call1());
    }

    public void B1Call2()
    {
        _b2Service.B2Call2().Wait();
        _b3Service.B3Call2().Wait();
    }
}