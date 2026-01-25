using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
    [EntityTypeConfiguration(typeof(SubdivisionConfiguration))]
    public class Subdivision
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }
        public bool AppendHeadName { get; set; } = false;
		public virtual List<Post> Posts { get; set; } = new List<Post>();
		public virtual HashSet<Permission> Permissions { get; set; } = new HashSet<Permission>();
        public int? HeadId { get; set; }
		public virtual Subdivision? Head { get; set; }
		public virtual List<Subdivision> Subordinates { get; set; } = new List<Subdivision>();

		public Subdivision(string name, string? envRoleString = null)
		{
            if (envRoleString != null)
            {
                DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found"));
            }
            Name = name;
		}

        public Subdivision() { }

        public string GetFullName()
        {
            return AppendHeadName && Head != null ? Name + " " + Head.GetFullName() : Name;
        }

        public HashSet<Permission> GetPermissionsRecursive()
        {
            HashSet<Permission> permissions = [.. Permissions];
            foreach (Subdivision sub in Subordinates)
                foreach (Permission permission in sub.GetPermissionsRecursive())
                    permissions.Add(permission);
            return permissions;
        }
    }
}
