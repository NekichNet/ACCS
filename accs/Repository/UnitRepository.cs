using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class UnitRepository : IUnitRepository
    {
        private AppDbContext _context;

        public UnitRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Create(Unit unit)
        {
            _context.Units.Add(unit);
            _context.SaveChanges();
        }

        public async Task<Unit?> Read(string discordId)
        {
            return _context.Units.Find(discordId);
        }

        public async Task<List<Unit>> ReadAll()
        {
            return _context.Units.ToList();
        }

        public async Task Update(Unit unit)
        {
            _context.Units.Update(unit);
            _context.SaveChanges();
        }
    }
}
