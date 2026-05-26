using accs.Models.Enums;

namespace accs.Models.Database
{
    public abstract class PermissionCheckable
    {
        public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();

        public abstract HashSet<GivedPermission> GetGivedPermissionsRecursive();
        public abstract HashSet<Permission> GetPermissionsRecursive();

        public virtual HashSet<Permission> GetPermissions()
        {
            return GivedPermissions.Select(gp => gp.Permission).ToHashSet();
        }

        public bool HasPermission(PermissionType permissionType)
        {
            return GetPermissionsRecursive().Any(p => p.Type == permissionType);
		}
    }
}
