using accs.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.Statuses
{
	[Table("AssignedPosts")]
	public class AssignedPost : UnitState
	{
		public int PostId { get; set; }
		[JsonIgnore] public virtual Post Post { get; set; }
	}

	public class AssignedPostConfiguration : IEntityTypeConfiguration<AssignedPost>
	{
		public void Configure(EntityTypeBuilder<AssignedPost> builder)
		{
			builder.HasOne(ap => ap.Post).WithMany(p => p.AssignedPosts).HasForeignKey(ap => ap.PostId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
