using System.Threading.Tasks;

public interface IA1Service
{
    public int A1Call1();
    public string A1Call2();
}

public class A1Service : IA1Service
{
    public A1Service()
    {
        
    }

    public int A1Call1()
    {
        return 5;
    }

    public string A1Call2()
    {
        return "Bob";
    }
}