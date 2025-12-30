using accs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accs.Models.Configurations
{
    public class DocConfiguration : IEntityTypeConfiguration<Doc>
    {
		public void Configure(EntityTypeBuilder<Doc> builder)
		{
			builder.HasOne(d => d.Author).WithMany(u => u.OwnDocs);
			builder.HasMany(d => d.Units).WithMany(u => u.AssignedDocs);
		}
	}
}
