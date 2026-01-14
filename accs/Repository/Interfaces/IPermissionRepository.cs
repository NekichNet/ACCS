using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> ReadAllAsync();
        Task<Permission?> ReadAsync(PermissionType permissionType);
    }
}
