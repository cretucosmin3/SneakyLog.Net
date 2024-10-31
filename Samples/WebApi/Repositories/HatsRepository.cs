using System.Linq;
using System.Threading.Tasks;

public interface IHatsRepository
{
    public Task<(bool,Hat[])> TryFindHats(int personId);
}

public class HatsRepository : IHatsRepository
{
    public async Task<(bool, Hat[])> TryFindHats(int personId)
    {
        await Task.Delay(5);

        var hats = TestingData.Hats.Where(hat => hat.PersonId.Equals(personId)).ToArray();

        return (hats.Length > 0, hats);
    }
}