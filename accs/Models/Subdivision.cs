namespace accs.Models
{
	public class Subdivision
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }
		public List<Post> Posts { get; set; } = new List<Post>();
		public HashSet<Permission> Permissions { get; set; } = new HashSet<Permission>();

		public Subdivision(string envRoleString)
		{
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found"));
		}
	}
}
