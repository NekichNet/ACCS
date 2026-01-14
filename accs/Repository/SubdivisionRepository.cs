using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class SubdivisionRepository : ISubdivisionRepository
    {
        private AppDbContext _context;

        public SubdivisionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Subdivision subdivision)
        {
            _context.Subdivisions.Add(subdivision);
            _context.SaveChanges();
        }

        public async Task DeleteAsync(Subdivision subdivision)
        {
            _context.Subdivisions.Remove(subdivision);
            _context.SaveChanges();
        }

        public async Task<Subdivision?> ReadAsync(int id)
        {
            return _context.Subdivisions.Find(id);
        }

        public async Task<List<Subdivision>> ReadAllAsync()
        {
            return _context.Subdivisions.ToList();
        }

        public async Task UpdateAsync(Subdivision subdivision)
        {
            _context.Subdivisions.Update(subdivision);
            _context.SaveChanges();
        }
    }
}
