using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> ReadAll();
        Task<Permission?> Read(PermissionType permissionType);
    }
}
