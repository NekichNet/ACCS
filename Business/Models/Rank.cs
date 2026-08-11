using Business.Models.Abstraction;
using Business.Models.Enums;
using Business.Models.Interfaces;
using Business.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class Rank : IEntityWithDiscordRole, IEntityWithFiles
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
		[JsonIgnore] public virtual HashSet<GivedPermission<Rank>> GivedPermissions { get; set; } = new HashSet<GivedPermission<Rank>>();
		[JsonIgnore] public virtual List<AssignedRank> AssignedRanks { get; set; } = new List<AssignedRank>();

		/// <summary>
		/// Индекс, необходимый для сравнения званий и сортировки.
		/// </summary>
		[NotMapped] public int Index { get { return GetIndex(); } }

		public void InsertLower(Rank rank)
		{
			if (Lower != null)
			{
				Lower.HigherId = rank.Id;
				rank.LowerId = Lower.Id;
			}
			rank.HigherId = Id;
			LowerId = rank.Id;
		}

		public void InsertHigher(Rank rank)
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

		public HashSet<GivedPermission<Rank>> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission<Rank>> givedPermissions = [.. GivedPermissions];
			if (Lower != null)
				foreach (GivedPermission<Rank> givedPermission in
					Lower.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					givedPermissions.Add(givedPermission);
			return givedPermissions;
		}

		public HashSet<Permission> GetPermissionsRecursive()
        {
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Lower != null)
				foreach (GivedPermission<Rank> givedPermission in
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

        public string GetFilesFolderName()
        {
			return "ranks";
        }

		public List<Rank> GetAllHigherRecursive()
		{
			List<Rank> higherRanks = new List<Rank>();
			Rank? tempHigherRank = Higher;

			while (tempHigherRank != null)
			{
				higherRanks.Add(tempHigherRank);
				tempHigherRank = tempHigherRank.Higher;
			}

			return higherRanks;
		}

		public List<Rank> GetAllLowerRecursive()
		{
			List<Rank> lowerRanks = new List<Rank>();
			Rank? tempLowerRank = Lower;

			while (tempLowerRank != null)
			{
				lowerRanks.Add(tempLowerRank);
				tempLowerRank = tempLowerRank.Lower;
			}

			return lowerRanks;
		}

		public int GetIndex()
		{
			return GetAllLowerRecursive().Count;
		}
    }

	public class RankConfiguration : IEntityTypeConfiguration<Rank>
	{
		public void Configure(EntityTypeBuilder<Rank> builder)
		{
			builder.HasOne(r => r.Lower).WithOne(rp => rp.Higher).HasForeignKey<Rank>(rp => rp.HigherId).OnDelete(DeleteBehavior.SetNull);
			builder.HasOne(r => r.Higher).WithOne(rp => rp.Lower).HasForeignKey<Rank>(rp => rp.LowerId).OnDelete(DeleteBehavior.SetNull);
			builder.HasMany(r => r.GivedPermissions).WithOne(gp => gp.Entity).HasForeignKey(gp => gp.EntityId).OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(r => r.AssignedRanks).WithOne(ar => ar.Rank).HasForeignKey(r => r.RankId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
