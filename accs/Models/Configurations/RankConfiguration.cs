using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accs.Models.Configurations
{
    public class RankConfiguration : IEntityTypeConfiguration<Rank>
	{
		public void Configure(EntityTypeBuilder<Rank> builder)
		{
			builder.HasOne(r => r.Lower).WithOne(rp => rp.Higher);
		}
	}
}
