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

        public async Task Create(Subdivision subdivision)
        {
            _context.Subdivisions.Add(subdivision);
            _context.SaveChanges();
        }

        public async Task Delete(Subdivision subdivision)
        {
            _context.Subdivisions.Remove(subdivision);
            _context.SaveChanges();
        }

        public async Task<Subdivision?> Read(int id)
        {
            return _context.Subdivisions.Find(id);
        }

        public async Task<List<Subdivision>> ReadAll()
        {
            return _context.Subdivisions.ToList();
        }

        public async Task Update(Subdivision subdivision)
        {
            _context.Subdivisions.Update(subdivision);
            _context.SaveChanges();
        }
    }
}
