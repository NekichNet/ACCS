
using accs.Database;

namespace accs.Services
{
    public class RewardService : BusinessService
    {
        private readonly AppDbContext _db;

        public RewardService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }
    }
}
