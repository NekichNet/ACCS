using accs.Models.Enums;

namespace accs.Models.Interfaces
{
    public interface IEntityWithPermissions
    {
        public abstract HashSet<GivedPermission> GivedPermissions { get; set; }

        HashSet<GivedPermission> GetGivedPermissionsRecursive();
        HashSet<Permission> GetPermissionsRecursive();
        HashSet<Permission> GetPermissions();
        bool HasPermission(PermissionType permissionType);
    }
}
