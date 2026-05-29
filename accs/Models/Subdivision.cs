using accs.Models.Configurations;
using accs.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(SubdivisionConfiguration))]
	public class Subdivision : IEntityWithPermissions, IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Description { get; set; } = string.Empty;
		public bool AppendHeadName { get; set; } = false;
		public virtual List<Post> Posts { get; set; } = new List<Post>();
		public int? HeadId { get; set; }
		public virtual Subdivision? Head { get; set; }
		public virtual List<Subdivision> Subordinates { get; set; } = new List<Subdivision>();
        public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();
		public string Color { get; set; } = "#AAAAAA";
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }

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

		public override string ToString()
		{
			return Id.ToString() + " " + Name;
		}

		public override HashSet<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. GetPermissions()];
			permissions.Concat(Head.GetGivedPermissionsRecursive()
				.Where(gp => gp.Inherit)
				.Select(gp => gp.Permission));
			return permissions;
		}

		public override HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission> givedPermissions = [.. GivedPermissions];
			givedPermissions.Concat(Head.GetGivedPermissionsRecursive()
				.Where(gp => gp.Inherit));
			return givedPermissions;
		}

        public void UpdateRole()
        {
            if (DiscordRoleId != null)
            {
                // TODO: Send request to discord-bot api
            }
        }

        public void CheckRoleOnUser(ulong unitId)
        {
            if (DiscordRoleId != null)
            {
                // TODO: Send request to discord-bot api
            }
        }
    }
}
