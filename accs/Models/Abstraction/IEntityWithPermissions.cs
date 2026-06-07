using accs.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.Interfaces
{
    public interface IEntityWithPermissions
    {
        [ForeignKey("EntityId")]
        abstract HashSet<GivedPermission> GivedPermissions { get; set; }

        HashSet<GivedPermission> GetGivedPermissionsRecursive();
        HashSet<Permission> GetPermissionsRecursive();
        HashSet<Permission> GetPermissions();
        bool HasPermission(PermissionType permissionType);
    }
}
