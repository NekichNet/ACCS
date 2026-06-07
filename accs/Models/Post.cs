using accs.Models.Configurations;
using accs.Models.Enums;
using accs.Models.Interfaces;
using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(PostConfiguration))]
	public class Post : IEntityWithPermissions, IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool AppendSubdivisionName { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public string Color { get; set; }
		public ulong? DiscordRoleId { get; set; }
		public int? SubdivisionId { get; set; }
		[JsonIgnore] public virtual Subdivision? Subdivision { get; set; }
		public int MaxRankId { get; set; }
		[JsonIgnore] public virtual Rank MaxRank { get; set; }
		public int? HeadId { get; set; }
		[JsonIgnore] public virtual Post? Head { get; set; }
		[JsonIgnore] public virtual List<Post> Subordinates { get; set; } = new List<Post>();
		[JsonIgnore] public virtual HashSet<GivedPermission> GivedPermissions { get; set; } = new HashSet<GivedPermission>();
		[JsonIgnore] public virtual List<AssignedPost> AssignedPosts { get; set; } = new List<AssignedPost>();

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
			return JsonSerializer.Serialize(this);
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
