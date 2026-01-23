using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
{
	public class Unit
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public ulong DiscordId { get; set; }
		public string Nickname { get; set; }
		public ulong? SteamId { get; set; }
		public ushort RankUpCounter { get; set; }
		public Rank Rank { get; set; }
		public List<Doc> OwnDocs { get; set; }
		public List<Doc> AssignedDocs { get; set; }
		public List<Post> Posts { get; set; } = new List<Post>();
		public List<Reward> Rewards { get; set; } = new List<Reward>();
		public List<Activity> Activities { get; set; } = new List<Activity>();
		public List<UnitStatus> UnitStatuses { get; set; } = new List<UnitStatus>();

        public HashSet<Permission> GetPermissions()
		{
			HashSet<Permission> permissions = Rank.GetPermissionsRecursive();
			foreach (Post post in Posts)
				foreach (Permission permission in post.GetPermissionsRecursive())
					permissions.Add(permission);
			return permissions;
		}

		public bool HasPermission(PermissionType permissionType)
		{
			return GetPermissions().Where(p => p.Type == permissionType || p.Type == PermissionType.Administrator).Any();
		}
	}
}
