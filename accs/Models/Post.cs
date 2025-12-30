namespace accs.Models
{
	public class Post
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public Subdivision? Subdivision { get; set; }
		public string? DiscordRoleId { get; set; }
		public List<Permission> Permissions { get; set; } = new List<Permission>();
		public List<UnitPost>
		public Post? Head { get; set; }
		public List<Post> Subordinates { get; set; } = new List<Post>();
	}
}
