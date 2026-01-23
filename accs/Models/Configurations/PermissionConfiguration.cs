using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accs.Models.Configurations
{
 //   public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
	//{
	//	public void Configure(EntityTypeBuilder<Permission> builder)
	//	{
	//		builder.HasMany(p => p.Ranks)
	//			.WithMany(r => r.Permissions)
	//			.UsingEntity<Dictionary<int, PermissionType>>(
	//				right => right
	//					.HasOne<Rank>()
	//					.WithMany()
	//					.HasForeignKey("RankId")
	//					.HasPrincipalKey(r => r.Id),
	//				left => left
	//					.HasOne<Permission>()
	//					.WithMany()
	//					.HasForeignKey("PermissionId")
	//					.HasPrincipalKey(p => p.Type),
	//				join => join
	//					.ToTable("PermissionRank")
	//					.HasKey("RankId", "PermissionId")
	//			);
	//		builder.HasMany(p => p.Posts)
	//			.WithMany(p => p.Permissions)
	//			.UsingEntity<Dictionary<int, PermissionType>>(
	//				right => right
	//					.HasOne<Post>()
	//					.WithMany()
	//					.HasForeignKey("PostId")
	//					.HasPrincipalKey(p => p.Id),
	//				left => left
	//					.HasOne<Permission>()
	//					.WithMany()
	//					.HasForeignKey("PermissionId")
	//					.HasPrincipalKey(p => p.Type),
	//				join => join
	//					.ToTable("PermissionPost")
	//					.HasKey("PostId", "PermissionId")
	//			);
	//		builder.HasMany(p => p.Subdivisions)
	//			.WithMany(s => s.Permissions)
	//			.UsingEntity<Dictionary<int, PermissionType>>(
	//				right => right
	//					.HasOne<Subdivision>()
	//					.WithMany()
	//					.HasForeignKey("SubdivisionId")
	//					.HasPrincipalKey(s => s.Id),
	//				left => left
	//					.HasOne<Permission>()
	//					.WithMany()
	//					.HasForeignKey("PermissionId")
	//					.HasPrincipalKey(p => p.Type),
	//				join => join
	//					.ToTable("PermissionSubdivision")
	//					.HasKey("SubdivisionId", "PermissionId")
	//			);
	//	}
	//}
}
