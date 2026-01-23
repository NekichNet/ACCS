using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class RankRepository : IRankRepository
    {
        private AppDbContext _context;

        public RankRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Rank rank)
        {
            _context.Ranks.Add(rank);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Rank rank)
        {
            _context.Ranks.Remove(rank);
            await _context.SaveChangesAsync();
        }

        public async Task<Rank?> ReadAsync(int id)
        {
            return await _context.Ranks.FindAsync(id);
        }

        public async Task<List<Rank>> ReadAllAsync()
        {
            return _context.Ranks.ToList();
        }

        public async Task UpdateAsync(Rank rank)
        {
            _context.Ranks.Update(rank);
            await _context.SaveChangesAsync();
        }
    }
}
