using System.Linq;
using System.Threading.Tasks;

public interface IPersonRepository
{
    public Task<(bool, Person?)> TryFindPerson(string name);
}

public class PersonRepository : IPersonRepository
{
    private readonly ICarsRepository _carsRepo;

    public PersonRepository(ICarsRepository carsRepo)
    {
        _carsRepo = carsRepo;
    }

    public async Task<(bool, Person?)> TryFindPerson(string name)
    {
        await Task.Delay(0);
        string lowerName = name.ToLower();

        var person = TestingData.People.FirstOrDefault(p => p.Name.ToLower().Contains(lowerName));

        if (person != null && _carsRepo.TryFindCars(person.Id, out var cars))
        {
            person.Cars = cars;
        }

        return (person != null, person);
    }
}