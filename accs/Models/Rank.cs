namespace accs.Models
{
	public class Rank
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string? DiscordRoleId { get; set; }
		public List<Permission> Permissions { get; set; } = new List<Permission>();
		public List<Unit> Units { get; set; } = new List<Unit>();
		public Rank? Subordinate { get; set; } // Звание на 1 ступень младше
		public Rank? Head { get; set; } // Звание на 1 ступень старше
	}
}
