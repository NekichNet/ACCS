using accs.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models
{
    public class GivedPermission
    {
        public int Id { get; set; }
        public PermissionType PermissionType { get; set; }
        [ForeignKey("PermissionType")]
        [JsonIgnore] public virtual Permission Permission { get; set; }
        public int EntityId { get; set; }
        public bool Inherit { get; set; } = true;
    }
}
