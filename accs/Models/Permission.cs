using accs.Models.Enums;
using accs.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[AttributeUsage(AttributeTargets.Field)]
	public class Permission : Attribute
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public PermissionType Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		[JsonIgnore] public virtual List<GivedPermission<Post>> PostPermissions { get; set; } = new List<GivedPermission<Post>>();
		[JsonIgnore] public virtual List<GivedPermission<Rank>> RankPermissions { get; set; } = new List<GivedPermission<Rank>>();
		[JsonIgnore] public virtual List<GivedPermission<Subdivision>> SubdivisionPermissions { get; set; } = new List<GivedPermission<Subdivision>>();

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
