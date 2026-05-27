using accs.Models.Database.Interfaces;
using accs.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.Database
{
    public class GivedPermission
    {
        public int Id { get; set; }
        public PermissionType PermissionType { get; set; }
        [ForeignKey("PermissionType")]
        public virtual Permission Permission { get; set; }
        public virtual IEntityWithPermissions Entity { get; set; }
        public bool Inherit { get; set; } = true;
    }
}
