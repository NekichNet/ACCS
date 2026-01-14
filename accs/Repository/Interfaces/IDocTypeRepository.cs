using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IDocTypeRepository
    {
        Task CreateAsync(DocType docType);
        Task<List<DocType>> ReadAllAsync();
        Task<DocType?> ReadAsync(int id);
        Task UpdateAsync(DocType docType);
        Task DeleteAsync(DocType docType);
    }
}
