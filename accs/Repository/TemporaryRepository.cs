using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class TemporaryRepository : ITemporaryRepository
    {
        private AppDbContext _context;

        public TemporaryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Create(UnitStatus temporary)
        {
            _context.Temporaries.Add(temporary);
            _context.SaveChanges();
        }

        public async Task Delete(UnitStatus temporary)
        {
            _context.Temporaries.Remove(temporary);
            _context.SaveChanges();
        }

        public async Task<UnitStatus?> Read(int id)
        {
            return _context.Temporaries.Find(id);
        }

        public async Task<List<UnitStatus>> ReadAll()
        {
            return _context.Temporaries.ToList();
        }

        public async Task Update(UnitStatus temporary)
        {
            _context.Temporaries.Update(temporary);
            _context.SaveChanges();
        }
    }
}
