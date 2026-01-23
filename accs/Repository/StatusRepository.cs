using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class StatusRepository : IStatusRepository
    {
        private AppDbContext _appDbContext;

        public StatusRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Status?> ReadAsync(StatusType statusType)
        {
            return await _appDbContext.Statuses.FindAsync(statusType);
        }
    }
}
