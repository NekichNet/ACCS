using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accs.Models.Database.Configurations
{
    public class AssignedRewardConfiguration : IEntityTypeConfiguration<AssignedReward>
	{
		public void Configure(EntityTypeBuilder<AssignedReward> builder)
		{
			builder.HasKey(a => new { a.RewardId, a.UnitId });
		}
	}
}
