using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class RewardRepository : IRewardRepository
    {
        private AppDbContext _context;

        public RewardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Reward reward)
        {
            _context.Rewards.Add(reward);
            _context.SaveChanges();
        }

        public async Task DeleteAsync(Reward reward)
        {
            _context.Rewards.Remove(reward);
            _context.SaveChanges();
        }

        public async Task<Reward?> ReadAsync(int id)
        {
            return _context.Rewards.Find(id);
        }

        public async Task<List<Reward>> ReadAllAsync()
        {
            return _context.Rewards.ToList();
        }

        public async Task UpdateAsync(Reward reward)
        {
            _context.Rewards.Update(reward);
            _context.SaveChanges();
        }
    }
}
