using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class DocTypeRepository : IDocTypeRepository
    {
        private AppDbContext _context;

        public DocTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Create(DocType docType)
        {
            _context.DocTypes.Add(docType);
            _context.SaveChanges();
        }

        public async Task Delete(DocType docType)
        {
            _context.DocTypes.Remove(docType);
            _context.SaveChanges();
        }

        public async Task<DocType?> Read(int id)
        {
            return _context.DocTypes.Find(id);
        }

        public async Task<List<DocType>> ReadAll()
        {
            return _context.DocTypes.ToList();
        }

        public async Task Update(DocType docType)
        {
            _context.DocTypes.Update(docType);
            _context.SaveChanges();
        }
    }
}
