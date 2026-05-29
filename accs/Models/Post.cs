using accs.Database;
using accs.Models.Configurations;
using accs.Models.Enums;
using accs.Models.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(PostConfiguration))]
	public class Post : IEntityWithPermissions, IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Description { get; set; } = string.Empty;
		public int? SubdivisionId { get; set; }
		public virtual Subdivision? Subdivision { get; set; }
		public bool AppendSubdivisionName { get; set; } = false;
		public int? HeadId { get; set; }
		public virtual Post? Head { get; set; }
		public virtual List<Post> Subordinates { get; set; } = new List<Post>();
		public int MaxRankId { get; set; }
		public virtual Rank MaxRank { get; set; }
		public virtual List<Unit> Units { get; set; } = new List<Unit>();
        public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();
        public string Color { get; set; }
		public string Name { get; set; }
		public ulong? DiscordRoleId { get; set; }

		public Post(string envRoleString)
		{
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found"));
		}

		public Post() { }

		public string GetFullName()
		{
			return Subdivision != null && AppendSubdivisionName ? Name + " " + Subdivision.GetFullName() : Name;
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

		public List<Post> GetAllHeadsRecursive()
		{
			List<Post> result = new List<Post>();
			Post? tempHead = Head;

			while (tempHead != null)
			{
				result.Add(tempHead);
				tempHead = tempHead.Head;
			}

			return result;
		}

		public Subdivision? GetHighestLevelSubdivision()
		{
			Subdivision? currentSubdivision = Subdivision;

			if (currentSubdivision != null)
				while (currentSubdivision.Head != null)
					currentSubdivision = currentSubdivision.Head;

			return currentSubdivision;
		}

		public override string ToString()
		{
			return Id.ToString() + " " + Name;
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

		public HashSet<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Subdivision != null)
				permissions.Concat(Subdivision.GetPermissionsRecursive());
			permissions.Concat(Subordinates.SelectMany(
				s => s.GetGivedPermissionsRecursive()
					.Where(gp => gp.Inherit)
					.Select(gp => gp.Permission)
				));
			return permissions;
		}

		public HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission> givedPermissions = [.. GivedPermissions];
			if (Subdivision != null)
				givedPermissions.Concat(Subdivision.GetGivedPermissionsRecursive());
			givedPermissions.Concat(Subordinates.SelectMany(
				s => s.GetGivedPermissionsRecursive()
					.Where(gp => gp.Inherit)
				));
			return givedPermissions;
		}

		public HashSet<Permission> GetPermissions()
        {
            return GivedPermissions.Select(gp => gp.Permission).ToHashSet();
        }

        public bool HasPermission(PermissionType permissionType)
        {
			return GetPermissionsRecursive().Any(p => p.Type == permissionType);
        }
    }
}
