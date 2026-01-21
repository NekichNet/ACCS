using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace accs.Repository
{
    public class ActivityRepository : IActivityRepository
    {
        private AppDbContext _context;

        public ActivityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Activity activity)
        {
			_context.Activities.Add(activity);
			_context.SaveChanges();
        }

        public async Task<List<Activity>> ReadAsync()
        {
            return _context.Activities.ToList();
        }

        public async Task<List<Activity>> ReadAllWithDateAsync(DateOnly date)
        {
            return _context.Activities.Where(a => a.Date == date).ToList();
        }

        public async Task<bool> ExistsAsync(Activity activity)
        {
            return _context.Activities.Contains(activity);
        }
    }
}
