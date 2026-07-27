using Business.Models.States.Abstraction;
using Business.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Business.Models.Statuses
{
	[Table("AssignedRanks")]
    public class AssignedRank : StateWithDoc
    {
        public int RankId { get; set; }
        [JsonIgnore] public virtual Rank Rank { get; set; }

		public override string GetText()
		{
			return $"Присвоено звание {Rank.Name}";
		}
	}

	public class AssignedRankConfiguration : IEntityTypeConfiguration<AssignedRank>
	{
		public void Configure(EntityTypeBuilder<AssignedRank> builder)
		{
			builder.HasOne(ar => ar.Rank).WithMany(r => r.AssignedRanks).HasForeignKey(ar => ar.RankId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
