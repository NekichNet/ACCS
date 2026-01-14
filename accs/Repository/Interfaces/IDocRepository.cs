using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IDocRepository
    {
        Task CreateAsync(Doc doc);
        Task<List<Doc>> ReadAllAsync();
        Task<Doc?> ReadAsync(int id);
        Task UpdateAsync(Doc doc);
        Task DeleteAsync(Doc doc);
    }
}
