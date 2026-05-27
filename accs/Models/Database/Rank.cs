using accs.Models.Database.Configurations;
using accs.Models.Database.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Database
{
	[EntityTypeConfiguration(typeof(RankConfiguration))]
	public class Rank : IEntityWithPermissions, IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public ushort CounterToReach { get; set; }
		public int? PreviousId { get; set; }
		public virtual Rank? Previous { get; set; }
		public int? NextId { get; set; }
		public virtual Rank? Next { get; set; }
		public virtual List<Unit> Units { get; set; } = new List<Unit>();
		public string Color { get; set; } = "#00FF00";
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }
		public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();

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
			rank.NextId = this.Id;
			PreviousId = rank.Id;
		}

		public void InsertNext(Rank rank)
		{
			if (Next != null)
			{
				Next.PreviousId = rank.Id;
				rank.NextId = Next.Id;
			}
			rank.PreviousId = this.Id;
			NextId = rank.Id;
		}

        public override string ToString()
        {
            return Id.ToString() + " " + Name;
        }

		public override HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission> givedPermissions = [.. GivedPermissions];
			if (Previous != null)
				foreach (GivedPermission givedPermission in
					Previous.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					givedPermissions.Add(givedPermission);
			return givedPermissions;
		}

		public override HashSet<Permission> GetPermissionsRecursive()
        {
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Previous != null)
				foreach (GivedPermission givedPermission in
					Previous.GetGivedPermissionsRecursive().Where(gp => gp.Inherit))
					permissions.Add(givedPermission.Permission);
			return permissions;
		}
    }
}
