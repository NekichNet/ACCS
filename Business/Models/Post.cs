using Business.Models.Enums;
using Business.Models.Interfaces;
using Business.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class Post : IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool AppendSubdivisionName { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public string Color { get; set; }
		public ulong? DiscordRoleId { get; set; }
		public int MaxRankId { get; set; }
		[JsonIgnore] public virtual Rank MaxRank { get; set; }
		public int? SubdivisionId { get; set; }
		[JsonIgnore] public virtual Subdivision? Subdivision { get; set; }
		public int? HeadId { get; set; }
		[JsonIgnore] public virtual Post? Head { get; set; }
		[JsonIgnore] public virtual List<Post> Subordinates { get; set; } = new List<Post>();
		[JsonIgnore] public virtual HashSet<GivedPermission<Post>> GivedPermissions { get; set; } = new HashSet<GivedPermission<Post>>();
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
			HashSet<Permission> permissions = [.. GivedPermissions.Select(gp => gp.Permission)];
			if (Subdivision != null)
				permissions.Concat(Subdivision.GetPermissionsRecursive());
			permissions.Concat(Subordinates.SelectMany(
				s => s.GetInheritedPermissionsRecursive()
				));
			return permissions;
		}

		private HashSet<Permission> GetInheritedPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. GivedPermissions.Where(gp => gp.Inherit).Select(gp => gp.Permission)];
			if (Subdivision != null)
				permissions.Concat(Subdivision.GetGivedPermissionsRecursive().Where(gp => gp.Inherit).Select(gp => gp.Permission));
			foreach (Post sub in Subordinates)
				permissions.Concat(sub.GetInheritedPermissionsRecursive());
			return permissions;
		}

		public bool HasPermission(PermissionType permissionType)
        {
			return GetPermissionsRecursive().Any(p => p.Type == permissionType);
        }

		public int GetIndex()
		{
			return GetAllHeadsRecursive().Count;
		}

		public PostDto ToDto()
		{
			return new PostDto
			{
				Id = Id,
				Name = Name,
				Fullname = GetFullName(),
				Index = GetIndex(),
				AppendSubdivisionName = AppendSubdivisionName,
				Description = Description,
				Color = Color,
				MaxRankId = MaxRankId,
				SubdivisionId = SubdivisionId,
				HeadId = HeadId,
				Permissions = GivedPermissions.Select(gp => gp.Permission).ToHashSet(),
				AllPermissions = GetPermissionsRecursive()
			};
		}
    }

	/// <summary>
	/// Общепринятое DTO для должности на импорт и экспорт
	/// </summary>
	public class PostDto
	{
		public int? Id { get; set; }
		/// <summary>
		/// Название только самой должности.
		/// Отображать только при наличии возможности редактирования.
		/// </summary>
		public string Name { get; set; } = string.Empty;
		/// <summary>
		/// Полное название должности, включающее названия подразделений.
		/// Отличается от Name, только если AppendSubdivisionName == true.
		/// Это свойство только на экспорт с бэкенда!
		/// </summary>
		public string Fullname { get; set; } = string.Empty;
		/// <summary>
		/// Индекс должности, необходимый для сортировки.
		/// </summary>
		public int Index { get; set; }
		public bool AppendSubdivisionName { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public string Color { get; set; } = string.Empty;
		public int MaxRankId { get; set; }
		public int? SubdivisionId { get; set; }
		public int? HeadId { get; set; }
		/// <summary>
		/// Разрешения, выданные конкретно этой должности
		/// </summary>
		public HashSet<Permission> Permissions { get; set; } = new();
		/// <summary>
		/// Все разрешения этой должности,
		/// в том числе унаследованные от подчинённых.
		/// </summary>
		public HashSet<Permission> AllPermissions { get; set; } = new();

		public override string ToString()
		{
			return JsonSerializer.Serialize(this);
		}
	}

	public class PostConfiguration : IEntityTypeConfiguration<Post>
	{
		public void Configure(EntityTypeBuilder<Post> builder)
		{
			builder.HasOne(p => p.Head).WithMany(ph => ph.Subordinates).HasForeignKey(ph => ph.HeadId).OnDelete(DeleteBehavior.SetNull);
			builder.HasMany(p => p.Subordinates).WithOne(ps => ps.Head).OnDelete(DeleteBehavior.SetNull);
			builder.HasOne(p => p.Subdivision).WithMany(s => s.Posts).HasForeignKey(p => p.SubdivisionId).OnDelete(DeleteBehavior.SetNull);
			builder.HasOne(p => p.MaxRank).WithMany().HasForeignKey(p => p.MaxRankId).OnDelete(DeleteBehavior.NoAction);
			builder.HasMany(p => p.GivedPermissions).WithOne().OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(p => p.AssignedPosts).WithOne(ap => ap.Post).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
