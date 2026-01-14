using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class UnitStatusRepository : IUnitStatusRepository
    {
        private AppDbContext _context;

        public UnitStatusRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Add(unitStatus);
            _context.SaveChanges();
        }

        public async Task DeleteAsync(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Remove(unitStatus);
            _context.SaveChanges();
        }

        public async Task<UnitStatus?> ReadAsync(int id)
        {
            return _context.UnitStatuses.Find(id);
        }

        public async Task<List<UnitStatus>> ReadAllAsync()
        {
            return _context.UnitStatuses.ToList();
        }

        public async Task UpdateAsync(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Update(unitStatus);
            _context.SaveChanges();
        }
    }
}
