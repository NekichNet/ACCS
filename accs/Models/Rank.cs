using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(RankConfiguration))]
	public class Rank
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }
		public ushort CounterToReach { get; set; }
		public int? PreviousId { get; set; }
		public Rank? Previous { get; set; }
		public int? NextId { get; set; }
		public Rank? Next { get; set; }
		public HashSet<Permission> Permissions { get; set; } = new HashSet<Permission>();
		public List<Unit> Units { get; set; } = new List<Unit>();

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

		public HashSet<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. Permissions];
			if (Previous != null)
				foreach (Permission permission in Previous.GetPermissionsRecursive())
					permissions.Add(permission);
			return permissions;
		}
	}
}
