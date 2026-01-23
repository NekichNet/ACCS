using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(PostConfiguration))]
	public class Post
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public int? SubdivisionId { get; set; }
		public Subdivision? Subdivision { get; set; }
		public ulong? DiscordRoleId { get; set; }
        public bool AppendSubdivisionName { get; set; } = false;
		public int? HeadId{ get; set; }
        public Post? Head { get; set; }
		public List<Post> Subordinates { get; set; } = new List<Post>();
		public List<Permission> Permissions { get; set; } = new List<Permission>();
		public List<Unit> Units { get; set; } = new List<Unit>();

		public Post(string envRoleString, List<Permission>? permissions = null)
		{
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found"));
			if (permissions != null)
				foreach (Permission permission in permissions)
					Permissions.Add(permission);
		}

		public Post() { }

		public string GetFullName()
		{
			return Subdivision != null && AppendSubdivisionName ? Name + " " + Subdivision.GetFullName() : Name;
		}

		public List<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. Permissions];
			if (Subdivision != null)
				foreach (Permission permission in Subdivision.Permissions)
					permissions.Add(permission);
			foreach (Post sub in Subordinates)
				foreach (Permission permission in sub.GetPermissionsRecursive())
					permissions.Add(permission);
			return permissions.ToList();
		}


        public List<Post> GetAllSubordinatesRecursive()
        {
            List<Post> result = [.. Subordinates];

            foreach (Post sub in Subordinates)
            {
                result.AddRange(sub.GetAllSubordinatesRecursive());
            }

            return result;
        }
    }
}
