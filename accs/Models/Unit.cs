using accs.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

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
		public DateTime Joined { get; set; }
		public int RankId { get; set; }
		[JsonIgnore] public virtual Rank Rank { get; set; }
		[JsonIgnore] public virtual List<Doc> OwnDocs { get; set; }
		[JsonIgnore] public virtual List<Doc> AssignedDocs { get; set; }
		[JsonIgnore] public virtual List<Post> Posts { get; set; } = new List<Post>();
		[JsonIgnore] public virtual List<AssignedReward> AssignedRewards { get; set; } = new List<AssignedReward>();
		[JsonIgnore] public virtual List<Activity> Activities { get; set; } = new List<Activity>();
		[JsonIgnore] public virtual List<UnitStatus> UnitStatuses { get; set; } = new List<UnitStatus>();

        public HashSet<Permission> GetPermissions()
		{
			HashSet<Permission> permissions = Rank.GetPermissionsRecursive();
			permissions.Concat(Posts.SelectMany(p => p.GetPermissionsRecursive()));
			return permissions;
		}

		public bool HasPermission(PermissionType permissionType)
		{
			return GetPermissions().Where(p => p.Type == permissionType || p.Type == PermissionType.Administrator).Any();
		}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}
