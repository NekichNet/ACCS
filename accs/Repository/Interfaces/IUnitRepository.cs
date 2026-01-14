using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IUnitRepository
    {
        Task CreateAsync(Unit unit);
        Task<List<Unit>> ReadAllAsync();
        Task<Unit?> ReadAsync(ulong discordId);
        Task UpdateAsync(Unit unit);
    }
}
