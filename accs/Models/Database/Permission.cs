using accs.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.Database
{
	public class Permission
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public PermissionType Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public virtual List<GivedPermission> GivedPermissions { get; set; } = new List<GivedPermission>();

        public override string ToString()
        {
            return Type.ToString();
        }
	}
}
