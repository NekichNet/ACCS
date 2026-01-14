using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IRankRepository
    {
        Task CreateAsync(Rank rank);
        Task<List<Rank>> ReadAllAsync();
        Task<Rank?> ReadAsync(int id);
        Task UpdateAsync(Rank rank);
        Task DeleteAsync(Rank rank);
    }
}
