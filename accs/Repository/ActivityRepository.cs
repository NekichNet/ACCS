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
            Activity? checkActivity = await ReadAsync(activity.Unit, activity.Date);
            if (checkActivity != null)
                checkActivity.Confirmed = checkActivity.Confirmed || activity.Confirmed;
            else
                _context.Activities.Add(activity);
            _context.SaveChanges();
        }

        public async Task<List<Activity>> ReadAllAsync()
        {
            return _context.Activities.ToList();
        }

        public async Task<List<Activity>> ReadAllWithDateAsync(DateOnly date)
        {
            return _context.Activities.Where(a => a.Date == date).ToList();
        }

        public async Task<Activity?> ReadAsync(Unit unit, DateOnly date)
        {
            return _context.Activities.Find(new { unit, date });
        }
    }
}
