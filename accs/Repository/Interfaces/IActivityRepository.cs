using accs.Models;

namespace accs.Repository.Interfaces
{
	public interface IActivityRepository
	{
		Task Create(Activity activity);
		Task<List<Activity>> ReadAll();
		Task<List<Activity>> ReadByDate(DateOnly date);
		Task<bool> Exists(Activity activity);
	}
}
