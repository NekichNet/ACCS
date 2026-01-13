using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IUnitStatusRepository
    {
        Task Create(UnitStatus temporary);
        Task<List<UnitStatus>> ReadAll();
        Task<UnitStatus?> Read(int id);
        Task Update(UnitStatus temporary);
        Task Delete(UnitStatus temporary);
    }
}
