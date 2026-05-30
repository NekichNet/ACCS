using accs.Models.Configurations;
using accs.Models.Enums;
using accs.Models.Interfaces;
using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(RankConfiguration))]
	public class Rank : IEntityWithPermissions, IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public ushort CounterToReach { get; set; }
		public int? PreviousId { get; set; }
		[JsonIgnore] public virtual Rank? Previous { get; set; }
		public int? NextId { get; set; }
		[JsonIgnore] public virtual Rank? Next { get; set; }
		public string Color { get; set; } = "#00FF00";
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }
		[JsonIgnore] public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();
		[JsonIgnore] public virtual List<AssignedRank> AssignedRanks { get; set; } = new List<AssignedRank>();

		public Rank(int id, string name, ushort counterToReach = 5)
		{
			Id = id;
			Name = name;
			CounterToReach = counterToReach;
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString($"RANK{Id}_ROLE_ID", $"RANK{Id}_ROLE_ID Not found"));
		}

		public Rank() { }

		public void InsertPrevious(Rank rank)
		{
			if (Previous != null)
			{
				Previous.NextId = rank.Id;
				rank.PreviousId = Previous.Id;
			}
			rank.NextId = Id;
			PreviousId = rank.Id;
		}

		public void InsertNext(Rank rank)
		{
			if (Next != null)
			{
				Next.PreviousId = rank.Id;
				rank.NextId = Next.Id;
			}
			rank.PreviousId = Id;
			NextId = rank.Id;
		}

        public override string ToString()
        {
            return Id.ToString() + " " + Name;
        }

		public HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission> givedPermissions = [.. GivedPermissions];
			if (Previous != null)
				foreach (GivedPermission givedPermission in
					Previous.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					givedPermissions.Add(givedPermission);
			return givedPermissions;
		}

		public HashSet<Permission> GetPermissionsRecursive()
        {
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Previous != null)
				foreach (GivedPermission givedPermission in
					Previous.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					permissions.Add(givedPermission.Permission);
			return permissions;
		}

        public HashSet<Permission> GetPermissions()
        {
            return GivedPermissions.Select(gp => gp.Permission).ToHashSet();
        }

        public bool HasPermission(PermissionType permissionType)
        {
			return GetPermissionsRecursive().Any(p => p.Type == permissionType);
        }

		public void UpdateRole()
		{
			if (DiscordRoleId != null)
			{
				// TODO: Send request to discord-bot api
			}
		}

		public void CheckRoleOnUser(ulong unitId)
		{
			if (DiscordRoleId != null)
			{
				// TODO: Send request to discord-bot api
			}
		}
	}
}
