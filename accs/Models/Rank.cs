using accs.Models.Abstraction;
using accs.Models.Configurations;
using accs.Models.Enums;
using accs.Models.Interfaces;
using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(RankConfiguration))]
	public class Rank : IEntityWithPermissions, IEntityWithDiscordRole, IEntityWithFiles
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public ushort CounterToReach { get; set; }
		public string Color { get; set; } = "#00FF00";
		public ulong? DiscordRoleId { get; set; }
		public int? LowerId { get; set; }
		[JsonIgnore] public virtual Rank? Lower { get; set; }
		public int? HigherId { get; set; }
		[JsonIgnore] public virtual Rank? Higher { get; set; }
		[JsonIgnore] public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();
		[JsonIgnore] public virtual List<AssignedRank> AssignedRanks { get; set; } = new List<AssignedRank>();

		public void InsertPrevious(Rank rank)
		{
			if (Lower != null)
			{
				Lower.HigherId = rank.Id;
				rank.LowerId = Lower.Id;
			}
			rank.HigherId = Id;
			LowerId = rank.Id;
		}

		public void InsertNext(Rank rank)
		{
			if (Higher != null)
			{
				Higher.LowerId = rank.Id;
				rank.HigherId = Higher.Id;
			}
			rank.LowerId = Id;
			HigherId = rank.Id;
		}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

		public HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission> givedPermissions = [.. GivedPermissions];
			if (Lower != null)
				foreach (GivedPermission givedPermission in
					Lower.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					givedPermissions.Add(givedPermission);
			return givedPermissions;
		}

		public HashSet<Permission> GetPermissionsRecursive()
        {
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Lower != null)
				foreach (GivedPermission givedPermission in
					Lower.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
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

        public string GetImageFolderName()
        {
			return "ranks";
        }

		public List<Rank> GetAllHigherRecursive()
		{
			List<Rank> higherRanks = new List<Rank>();
			if (Higher != null)
			{
				higherRanks.Add(Higher);
				higherRanks.AddRange(Higher.GetAllHigherRecursive());
			}
			return higherRanks;
		}

		public List<Rank> GetAllLowerRecursive()
		{
			List<Rank> higherRanks = new List<Rank>();
			if (Lower != null)
			{
				higherRanks.Add(Lower);
				higherRanks.AddRange(Lower.GetAllHigherRecursive());
			}
			return higherRanks;
		}
    }
}
