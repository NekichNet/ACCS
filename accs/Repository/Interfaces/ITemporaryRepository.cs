using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface ITemporaryRepository
    {
        Task Create(Temporary temporary);
        Task<List<Temporary>> ReadAll();
        Task<Temporary?> Read(int id);
        Task Update(Temporary temporary);
        Task Delete(Temporary temporary);
    }
}
