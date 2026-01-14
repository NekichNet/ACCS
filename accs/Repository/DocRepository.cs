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

        public async Task CreateAsync(Doc doc)
        {
            _context.Docs.Add(doc);
            _context.SaveChanges();
        }

        public async Task DeleteAsync(Doc doc)
        {
            _context.Docs.Remove(doc);
            _context.SaveChanges();
        }
        
        public async Task<Doc?> ReadAsync(int id)
        {
            return _context.Docs.Find(id);
        }

        public async Task<List<Doc>> ReadAllAsync()
        {
            return _context.Docs.ToList();
        }

        public async Task UpdateAsync(Doc doc)
        {
            _context.Docs.Update(doc);
            _context.SaveChanges();
        }
    }
}
