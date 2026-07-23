using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
    public class Activity
	{
		public ulong UnitId { get; set; }
		[JsonIgnore] public virtual Unit Unit { get; set; }
		public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}

	public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
	{
		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.HasKey(a => new { a.UnitId, a.Date });
			builder.HasOne(a => a.Unit).WithMany(u => u.Activities).HasForeignKey(u => u.UnitId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
