using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IStatusRepository
    {
        Task<Status?> ReadAsync(StatusType statusType);
    }
}
