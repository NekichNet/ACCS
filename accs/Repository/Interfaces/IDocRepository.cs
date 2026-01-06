using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IDocRepository
    {
        Task Create(Doc doc);
        Task<List<Doc>> ReadAll();
        Task<Doc?> Read(int id);
        Task Update(Doc doc);
        Task Delete(Doc doc);
    }
}
