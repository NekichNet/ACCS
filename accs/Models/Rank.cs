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

        public Rank(int id, string name, ushort counterToReach = 5, HashSet<Permission>? permissions = null)
		{
			Id = id;
			Name = name;
			CounterToReach = counterToReach;
			if (permissions != null) Permissions = permissions;
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString($"RANK{Id}_ROLE_ID", $"RANK{Id}_ROLE_ID Not found"));
		}

		public Rank() { }

		public void InsertPrevious(Rank rank)
		{
			Previous?.Next = rank;
			rank.Previous = Previous;
			rank.Next = this;
			Previous = rank;
		}

		public void InsertNext(Rank rank)
		{
			Next?.Previous = rank;
			rank.Next = Next;
			rank.Previous = this;
			Next = rank;
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
