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
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Remove(unitStatus);
            await _context.SaveChangesAsync();
        }

        public async Task<UnitStatus?> ReadAsync(int id)
        {
            return await _context.UnitStatuses.FindAsync(id);
        }

        public async Task<List<UnitStatus>> ReadAllAsync()
        {
            return _context.UnitStatuses.ToList();
        }

        public async Task UpdateAsync(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Update(unitStatus);
            await _context.SaveChangesAsync();
        }
    }
}
