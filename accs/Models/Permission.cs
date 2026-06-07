using accs.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[AttributeUsage(AttributeTargets.Field)]
	public class Permission : Attribute
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public PermissionType Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		[JsonIgnore] public virtual List<GivedPermission> GivedPermissions { get; set; } = new List<GivedPermission>();

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}
