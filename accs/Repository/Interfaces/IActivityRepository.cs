using accs.Models;

namespace accs.Repository.Interfaces
{
	public interface IActivityRepository
	{
		Task CreateAsync(Activity activity);
		Task<List<Activity>> ReadAllAsync();
		Task<List<Activity>> ReadAllWithDateAsync(DateOnly date);
		Task<Activity?> ReadAsync(Unit unit, DateOnly date);
	}
}
