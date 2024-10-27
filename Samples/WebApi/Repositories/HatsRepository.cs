using System.Linq;

public interface IHatsRepository
{
    public bool TryFindHats(int personId, out Hat[]? hats);
}

public class HatsRepository : IHatsRepository
{
    public bool TryFindHats(int personId, out Hat[]? hats)
    {
        hats = TestingData.Hats.Where(hat => hat.PersonId.Equals(personId)).ToArray();
        return hats.Length > 0;
    }
}