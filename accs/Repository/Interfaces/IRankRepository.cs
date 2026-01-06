using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IRankRepository
    {
        Task Create(Rank rank);
        Task<List<Rank>> ReadAll();
        Task<Rank?> Read(int id);
        Task Update(Rank rank);
        Task Delete(Rank rank);
    }
}
