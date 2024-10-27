using System.Threading.Tasks;

public interface IPersonService
{
    public Task<Person?> FindPersonByName(string name);
}

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepo;
    private readonly IHatsRepository _hatsRepo;

    public PersonService(IPersonRepository personRepo, IHatsRepository hatsRepo)
    {
        _personRepo = personRepo;
        _hatsRepo = hatsRepo;
    }

    public async Task<Person?> FindPersonByName(string name)
    {
        (bool found, Person? person) = await _personRepo.TryFindPerson(name);
        
        if (!found || person == null) return null;

        if (_hatsRepo.TryFindHats(person.Id, out Hat[]? hats))
            person.Hats = hats;

        return person;
    }
}