using System.Linq;
using System.Threading.Tasks;

public interface IPersonRepository
{
    public Task<(bool, Person?)> TryFindPerson(string name);
}

public class PersonRepository : IPersonRepository
{
    public async Task<(bool, Person?)> TryFindPerson(string name)
    {
        await Task.Delay(5);
        string lowerName = name.ToLower();

        var person = TestingData.People.FirstOrDefault(p => p.Name.ToLower().Contains(lowerName));

        return (person != null, person);
    }
}