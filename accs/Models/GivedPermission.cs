using accs.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class GivedPermission<T>
    {
        public int Id { get; set; }
        public PermissionType PermissionType { get; set; }
        [JsonIgnore] public virtual Permission Permission { get; set; }
        public int EntityId { get; set; }
		[JsonIgnore] public virtual T Entity { get; set; }
        public bool Inherit { get; set; } = true;
    }

	public class PostPermissionConfiguration : IEntityTypeConfiguration<GivedPermission<Post>>
	{
		public void Configure(EntityTypeBuilder<GivedPermission<Post>> builder)
		{
			builder.HasOne(gp => gp.Permission).WithMany(p => p.PostPermissions).HasForeignKey(gp => gp.PermissionType).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(gp => gp.Entity).WithMany(p => p.GivedPermissions).HasForeignKey(gp => gp.EntityId).OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class RankPermissionConfiguration : IEntityTypeConfiguration<GivedPermission<Rank>>
	{
		public void Configure(EntityTypeBuilder<GivedPermission<Rank>> builder)
		{
			builder.HasOne(gp => gp.Permission).WithMany(p => p.RankPermissions).HasForeignKey(gp => gp.PermissionType).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(gp => gp.Entity).WithMany(p => p.GivedPermissions).HasForeignKey(gp => gp.EntityId).OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class SubdivisionPermissionConfiguration : IEntityTypeConfiguration<GivedPermission<Subdivision>>
	{
		public void Configure(EntityTypeBuilder<GivedPermission<Subdivision>> builder)
		{
			builder.HasOne(gp => gp.Permission).WithMany(p => p.SubdivisionPermissions).HasForeignKey(gp => gp.PermissionType).OnDelete(DeleteBehavior.NoAction);
			builder.HasOne(gp => gp.Entity).WithMany(p => p.GivedPermissions).HasForeignKey(gp => gp.EntityId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
