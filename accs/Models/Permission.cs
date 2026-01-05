namespace accs.Models
{
	public class Permission
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public List<Rank> Ranks { get; set; } = new List<Rank>();
		public List<Post> Posts { get; set; } = new List<Post>();
		public List<Subdivision> Subdivisions { get; set; } = new List<Subdivision>();
	}
}
