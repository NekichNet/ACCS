using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IDocTypeRepository
    {
        Task Create(DocType docType);
        Task<List<DocType>> ReadAll();
        Task<DocType?> Read(int id);
        Task Update(DocType docType);
        Task Delete(DocType docType);
    }
}
