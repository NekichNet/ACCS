using accs.Models.Enums;

namespace accs.Models.Database.Interfaces
{
    public interface IEntityWithPermissions
    {
        public abstract HashSet<GivedPermission> GivedPermissions { get; set; }

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
