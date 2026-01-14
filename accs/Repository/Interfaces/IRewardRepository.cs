using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IRewardRepository
    {
        Task CreateAsync(Reward reward);
        Task<List<Reward>> ReadAllAsync();
        Task<Reward?> ReadAsync(int id);
        Task UpdateAsync(Reward reward);
        Task DeleteAsync(Reward reward);
    }
}
