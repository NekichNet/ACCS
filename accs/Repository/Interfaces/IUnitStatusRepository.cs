using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IUnitStatusRepository
    {
        Task CreateAsync(UnitStatus unitStatus);
        Task<List<UnitStatus>> ReadAllAsync();
        Task<UnitStatus?> ReadAsync(int id);
        Task UpdateAsync(UnitStatus unitStatus);
        Task DeleteAsync(UnitStatus unitStatus);
    }
}
