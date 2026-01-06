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

        public async Task Create(Reward reward)
        {
            _context.Rewards.Add(reward);
            _context.SaveChanges();
        }

        public async Task Delete(Reward reward)
        {
            _context.Rewards.Remove(reward);
            _context.SaveChanges();
        }

        public async Task<Reward?> Read(int id)
        {
            return _context.Rewards.Find(id);
        }

        public async Task<List<Reward>> ReadAll()
        {
            return _context.Rewards.ToList();
        }

        public async Task Update(Reward reward)
        {
            _context.Rewards.Update(reward);
            _context.SaveChanges();
        }
    }
}
