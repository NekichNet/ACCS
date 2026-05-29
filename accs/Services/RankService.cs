
using accs.Database;

namespace accs.Services
{
    public class RankService : BusinessService
    {
        private readonly AppDbContext _db;
        public RankService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }
    }
}
