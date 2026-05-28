using accs.Models.Enums;
using accs.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
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
