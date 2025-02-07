using System.Linq;
using System.Threading.Tasks;
using SneakyLog.Utilities;

public interface IHatsRepository
{
    public Task<(bool,Hat[])> TryFindHats(int personId);
}

[SneakyLog]
public class HatsRepository : IHatsRepository
{
    public async Task<(bool, Hat[])> TryFindHats(int personId)
    {
        await Task.Delay(5);

        var hats = TestingData.Hats.Where(hat => hat.PersonId.Equals(personId)).ToArray();

        throw new System.Exception("Just testing...");

        return (hats.Length > 0, hats);
    }
}