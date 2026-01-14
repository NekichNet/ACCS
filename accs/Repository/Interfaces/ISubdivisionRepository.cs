using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface ISubdivisionRepository
    {
        Task CreateAsync(Subdivision subdivision);
        Task<List<Subdivision>> ReadAllAsync();
        Task<Subdivision?> ReadAsync(int id);
        Task UpdateAsync(Subdivision subdivision);
        Task DeleteAsync(Subdivision subdivision);
    }
}
