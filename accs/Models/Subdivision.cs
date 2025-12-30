namespace accs.Models
{
	public class Subdivision
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public string? DiscordRoleId { get; set; }
		public List<Post> Posts { get; set; } = new List<Post>();
		public List<Permission> Permissions { get; set; } = new List<Permission>();
	}
}
