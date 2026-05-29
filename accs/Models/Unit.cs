using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.Statuses;
using accs.Models.Statuses.Abstraction;
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
		public int RegistrationEventId { get; set; }
		[JsonIgnore] public virtual UnitRegistrationEvent RegistrationEvent { get; set; }
		[JsonIgnore] public virtual List<Doc> OwnDocs { get; set; }
		[JsonIgnore] public virtual List<Doc> AssignedDocs { get; set; }
		[JsonIgnore] public virtual List<AssignedReward> AssignedRewards { get; set; } = new List<AssignedReward>();
		[JsonIgnore] public virtual List<Activity> Activities { get; set; } = new List<Activity>();
		[JsonIgnore] public virtual List<AssignedRank> AssignedRanks { get; set; } = new List<AssignedRank>();
		[JsonIgnore] public virtual List<AssignedPost> AssignedPosts { get; set; } = new List<AssignedPost>();
		[JsonIgnore] public virtual List<UnitState> UnitStates { get; set; } = new List<UnitState>();

		public AssignedRank? GetAssignedRank()
		{
			return AssignedRanks.FirstOrDefault(ar => ar.IsActive());
		}

		public List<AssignedPost> GetAssignedPosts()
		{
			return AssignedPosts.Where(ap => ap.IsActive()).ToList();
		}

		public HashSet<Permission> GetPermissions()
		{
			HashSet<Permission> permissions = new HashSet<Permission>();

			AssignedRank? assignedRank = GetAssignedRank();
			if (assignedRank != null)
				permissions.Concat(assignedRank.Rank.GetPermissionsRecursive());

			permissions.Concat(GetAssignedPosts().SelectMany(ap => ap.Post.GetPermissionsRecursive()));
			return permissions;
		}

		public bool HasPermission(PermissionType permissionType)
		{
			return GetPermissions().Where(p => p.Type == permissionType || p.Type == PermissionType.Administrator).Any();
		}

		public void CheckRoles()
		{
			AssignedRank? assignedRank = GetAssignedRank();
			if (assignedRank != null)
				assignedRank.Rank.CheckRoleOnUser(DiscordId);
			foreach (AssignedPost assignedPost in AssignedPosts)
				assignedPost.Post.CheckRoleOnUser(DiscordId);
		}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}