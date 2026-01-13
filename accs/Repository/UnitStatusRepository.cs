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

        public async Task Create(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Add(unitStatus);
            _context.SaveChanges();
        }

        public async Task Delete(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Remove(unitStatus);
            _context.SaveChanges();
        }

        public async Task<UnitStatus?> Read(int id)
        {
            return _context.UnitStatuses.Find(id);
        }

        public async Task<List<UnitStatus>> ReadAll()
        {
            return _context.UnitStatuses.ToList();
        }

        public async Task Update(UnitStatus unitStatus)
        {
            _context.UnitStatuses.Update(unitStatus);
            _context.SaveChanges();
        }
    }
}
