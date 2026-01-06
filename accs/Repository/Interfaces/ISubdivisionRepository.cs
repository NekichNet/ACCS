using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface ISubdivisionRepository
    {
        Task Create(Subdivision subdivision);
        Task<List<Subdivision>> ReadAll();
        Task<Subdivision?> Read(int id);
        Task Update(Subdivision subdivision);
        Task Delete(Subdivision subdivision);
    }
}
