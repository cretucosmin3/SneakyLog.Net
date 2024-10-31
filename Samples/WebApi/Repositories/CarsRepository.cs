using System.Linq;
using System.Threading.Tasks;

public interface ICarsRepository
{
    public Task<(bool, Car[])> TryFindCars(int personId);
}

public class CarsRepository : ICarsRepository
{
    public async Task<(bool, Car[])> TryFindCars(int personId)
    {
        await Task.Delay(5);
        
        var cars = TestingData.Cars.Where(car => car.PersonId.Equals(personId)).ToArray();
        return (cars.Length > 0, cars);
    }
}