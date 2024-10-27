using System.Linq;

public interface ICarsRepository
{
    public bool TryFindCars(int personId, out Car[]? cars);
}

public class CarsRepository : ICarsRepository
{
    public bool TryFindCars(int personId, out Car[]? cars)
    {
        cars = TestingData.Cars.Where(car => car.PersonId.Equals(personId)).ToArray();
        return cars.Length > 0;
    }
}