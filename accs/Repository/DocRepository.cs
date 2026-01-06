using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class DocRepository : IDocRepository
    {
        private AppDbContext _context;

        public DocRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Create(Doc doc)
        {
            _context.Docs.Add(doc);
            _context.SaveChanges();
        }

        public async Task Delete(Doc doc)
        {
            _context.Docs.Remove(doc);
            _context.SaveChanges();
        }
        
        public async Task<Doc?> Read(int id)
        {
            return _context.Docs.Find(id);
        }

        public async Task<List<Doc>> ReadAll()
        {
            return _context.Docs.ToList();
        }

        public async Task Update(Doc doc)
        {
            _context.Docs.Update(doc);
            _context.SaveChanges();
        }
    }
}
