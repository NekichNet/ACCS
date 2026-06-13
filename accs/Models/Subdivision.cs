using accs.Models.Enums;
using accs.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class Subdivision : IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public bool AppendHeadName { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public string Color { get; set; } = "#AAAAAA";
		public ulong? DiscordRoleId { get; set; }
		public int? HeadId { get; set; }
		[JsonIgnore] public virtual Subdivision? Head { get; set; }
		[JsonIgnore] public virtual List<Subdivision> Subordinates { get; set; } = new List<Subdivision>();
		[JsonIgnore] public virtual List<Post> Posts { get; set; } = new List<Post>();
		[JsonIgnore] public virtual HashSet<GivedPermission<Subdivision>> GivedPermissions { get; set; } = new HashSet<GivedPermission<Subdivision>>();
		
		public string GetFullName()
		{
			return AppendHeadName && Head != null ? Name + " " + Head.GetFullName() : Name;
		}

		public override string ToString()
		{
			return JsonSerializer.Serialize(this);
		}

		public HashSet<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. GetPermissions()];
			permissions.Concat(Head.GetGivedPermissionsRecursive()
				.Where(gp => gp.Inherit)
				.Select(gp => gp.Permission));
			return permissions;
		}

		public HashSet<GivedPermission<Subdivision>> GetGivedPermissionsRecursive()
		{
			HashSet<GivedPermission<Subdivision>> givedPermissions = [.. GivedPermissions];
			if (Head != null)
				givedPermissions.Concat(Head.GetGivedPermissionsRecursive()
					.Where(gp => gp.Inherit));
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

	public class SubdivisionConfiguration : IEntityTypeConfiguration<Subdivision>
	{
		public void Configure(EntityTypeBuilder<Subdivision> builder)
		{
			builder.HasOne(s => s.Head).WithMany(sh => sh.Subordinates).HasForeignKey(s => s.HeadId).OnDelete(DeleteBehavior.SetNull);
			builder.HasMany(s => s.Posts).WithOne(p => p.Subdivision).OnDelete(DeleteBehavior.SetNull);
			builder.HasMany(s => s.GivedPermissions).WithOne().OnDelete(DeleteBehavior.Cascade);
		}
	}
}
