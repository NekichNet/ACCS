using accs.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.Statuses
{
	[EntityTypeConfiguration(typeof(AssignedRankConfiguration))]
	[Table("AssignedRanks")]
    public class AssignedRank : UnitState
    {
        public int RankId { get; set; }
        [JsonIgnore] public virtual Rank Rank { get; set; }
    }

	public class AssignedRankConfiguration : IEntityTypeConfiguration<AssignedRank>
	{
		public void Configure(EntityTypeBuilder<AssignedRank> builder)
		{
			builder.HasOne(ar => ar.Rank).WithMany(r => r.AssignedRanks).HasForeignKey(ar => ar.RankId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
