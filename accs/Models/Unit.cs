using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.SingleDayEvents.Abstraction;
using accs.Models.States;
using accs.Models.States.Abstraction;
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
		public int FavoriteKitId { get; set; } = 1;
		[JsonIgnore] public virtual FavoriteKit FavoriteKit { get; set; }
		public int BackgroundPictureId { get; set; }
		[JsonIgnore] public virtual BackgroundPicture BackgroundPicture { get; set; }
		[JsonIgnore] public virtual List<Doc> OwnDocs { get; set; }
		[JsonIgnore] public virtual List<Doc> AssignedDocs { get; set; }
		[JsonIgnore] public virtual List<AssignedReward> AssignedRewards { get; set; } = new List<AssignedReward>();
		[JsonIgnore] public virtual List<Activity> Activities { get; set; } = new List<Activity>();
		[JsonIgnore] public virtual List<AssignedRank> AssignedRanks { get; set; } = new List<AssignedRank>();
		[JsonIgnore] public virtual List<AssignedPost> AssignedPosts { get; set; } = new List<AssignedPost>();
		[JsonIgnore] public virtual List<Retirement> Retirements { get; set; }
		[JsonIgnore] public virtual List<Status> Statuses { get; set; } = new List<Status>();
		[JsonIgnore] public virtual List<UnitState> UnitStates { get; set; } = new List<UnitState>();
		[JsonIgnore] public virtual List<SingleDayEvent> SingleDayEvents { get; set; } = new List<SingleDayEvent>();
        
        public AssignedRank? GetAssignedRank(DateTime? dateTime = null)
		{
			dateTime = dateTime ?? DateTime.UtcNow;
			return AssignedRanks.FirstOrDefault(ar => ar.IsActive(dateTime));
		}

		public Rank? GetRank(DateTime? dateTime = null)
		{
			dateTime = dateTime ?? DateTime.UtcNow;
			AssignedRank? assignedRank = GetAssignedRank(dateTime);
			return assignedRank == null ? null : assignedRank.Rank;
		}

		public string GetRankName(DateTime? dateTime = null)
		{
			dateTime = dateTime ?? DateTime.UtcNow;
			AssignedRank? assignedRank = GetAssignedRank(dateTime);
			return assignedRank == null ? "Без звания" : assignedRank.Rank.Name;
		}

		public List<AssignedPost> GetAssignedPosts(DateTime? dateTime = null)
		{
			dateTime = dateTime ?? DateTime.UtcNow;
			return AssignedPosts.Where(ap => ap.IsActive(dateTime)).ToList();
		}

		public List<Post> GetPosts(DateTime? dateTime = null)
		{
			dateTime = dateTime ?? DateTime.UtcNow;
			return AssignedPosts.Where(ap => ap.IsActive(dateTime)).Select(ap => ap.Post).ToList();
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
			return GetPermissions().Any(p => p.Type == permissionType || p.Type == PermissionType.Administrator);
		}

		public bool IsAdmin()
		{
			return GetPermissions().Any(p => p.Type == PermissionType.Administrator);
		}

		public void CheckRoles()
		{
			AssignedRank? assignedRank = GetAssignedRank();
			if (assignedRank != null)
				assignedRank.Rank.CheckRoleOnUser(DiscordId);
			foreach (AssignedPost assignedPost in AssignedPosts)
				assignedPost.Post.CheckRoleOnUser(DiscordId);
		}

		/// <summary>
		/// Проверка, не уволен или не в отставке ли этот боец.
		/// </summary>
		/// <returns>
		/// true, если всё ещё в клане. false, если в отставке или уволен
		/// </returns>
		public bool IsActive()
		{
			return AssignedRanks.Any(ar => ar.IsActive()) && AssignedPosts.Any(ap => ap.IsActive());
		}

		/// <summary>
		/// Проверка, находится ли боец в отставке
		/// </summary>
		/// <returns>true, если в отставке</returns>
		public bool IsInRetirement()
		{
			return Retirements.Any(r => r.IsActive());
		}

		/// <summary>
		/// Получить максимальное звание которое может получить боец с текущими должностями
		/// </summary>
		/// <returns>Rank, если есть должности. null, если должностей нет</returns>
		public Rank? GetMaxRank()
		{
			Rank? rank = null;
			foreach (Post post in GetPosts())
			{
				if (rank != null)
				{
					if (rank.GetAllHigherRecursive().Contains(post.MaxRank))
						rank = post.MaxRank;
				}
				else
					rank = post.MaxRank;
			}
			return rank;
		}

		public List<Status> GetStatuses()
		{
			return Statuses.Where(s => s.IsActive()).ToList();
		}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}