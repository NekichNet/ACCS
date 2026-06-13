using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents.Abstraction
{
	[Table("Events")]
    public abstract class SingleDayEvent
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public ulong UnitId { get; set; }
        [JsonIgnore] public virtual Unit Unit { get; set; }

        public virtual string GetText()
        {
            return string.Empty;
        }
        public virtual string? GetHexColor()
        {
            return null;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

	public class SingleDayEventConfiguration : IEntityTypeConfiguration<SingleDayEvent>
	{
		public void Configure(EntityTypeBuilder<SingleDayEvent> builder)
		{
			builder.HasOne(e => e.Unit).WithMany(u => u.SingleDayEvents).HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
