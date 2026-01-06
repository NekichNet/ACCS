using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IRewardRepository
    {
        Task Create(Reward reward);
        Task<List<Reward>> ReadAll();
        Task<Reward?> Read(int id);
        Task Update(Reward reward);
        Task Delete(Reward reward);
    }
}
