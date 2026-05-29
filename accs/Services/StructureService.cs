
using accs.Database;

namespace accs.Services
{
    public class StructureService : BusinessService
    {
        private readonly AppDbContext _db;

        public StructureService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }
    }
}
