using System;
using System.Threading.Tasks;
using SneakyLog;

public interface IPersonService
{
    public Task<Person?> FindPersonByName(string name);
}

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepo;
    private readonly IHatsRepository _hatsRepo;
    private readonly ICarsRepository _carsRepo;

    public PersonService(IPersonRepository personRepo, IHatsRepository hatsRepo, ICarsRepository carsRepo)
    {
        _personRepo = personRepo;
        _hatsRepo = hatsRepo;
        _carsRepo = carsRepo;
    }

    public async Task<Person?> FindPersonByName(string name)
    {
        (bool found, Person? person) = await _personRepo.TryFindPerson(name);

        if (!found || person == null) return null;

        if (person != null)
        {
            try
            {
                var findHats = _hatsRepo.TryFindHats(person.Id);
                var findCars = _carsRepo.TryFindCars(person.Id);
                Task.WaitAll([findHats, findCars]);
                person.Hats = findHats.Result.Item2;
                person.Cars = findCars.Result.Item2;
            }
            catch (System.Exception)
            {
                string trace = SneakyLogContext.GetTrace();
                Console.WriteLine($"Endpoint finished with trace: {trace}");
            }
        }

        return person;
    }
}