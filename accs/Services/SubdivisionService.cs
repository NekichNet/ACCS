
using accs.Database;

namespace accs.Services
{
    public class SubdivisionService : BusinessService
    {
        private readonly AppDbContext _db;

        public SubdivisionService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }
    }
}
