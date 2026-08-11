using Business.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class Permission
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		[JsonIgnore] public PermissionType Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		[JsonIgnore] public virtual List<GivedPermission<Post>> PostPermissions { get; set; } = new List<GivedPermission<Post>>();
		[JsonIgnore] public virtual List<GivedPermission<Rank>> RankPermissions { get; set; } = new List<GivedPermission<Rank>>();
		[JsonIgnore] public virtual List<GivedPermission<Subdivision>> SubdivisionPermissions { get; set; } = new List<GivedPermission<Subdivision>>();

		[NotMapped] public int Id { get { return (int)Type; } }

		public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}

	public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
	{
		public void Configure(EntityTypeBuilder<Permission> builder)
		{
			builder.HasMany(p => p.PostPermissions).WithOne(pp => pp.Permission);
			builder.HasMany(p => p.RankPermissions).WithOne(pp => pp.Permission);
			builder.HasMany(p => p.SubdivisionPermissions).WithOne(pp => pp.Permission);
		}
	}
}
