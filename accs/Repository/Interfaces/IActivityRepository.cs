using accs.Models;

namespace accs.Repository.Interfaces
{
	public interface IActivityRepository
	{
		Task CreateAsync(Activity activity);
		Task<List<Activity>> ReadAsync();
		Task<List<Activity>> ReadAllWithDateAsync(DateOnly date);
		Task<bool> ExistsAsync(Activity activity);
	}
}
