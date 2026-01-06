using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IUnitRepository
    {
        Task Create(Unit unit);
        Task<List<Unit>> ReadAll();
        Task<Unit?> Read(string discordId);
        Task Update(Unit unit);
    }
}
