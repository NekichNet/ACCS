using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models.SingleDayEvents.Abstraction
{
	[Table("Events")]
    public abstract class SingleDayEvent
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        [JsonIgnore] public virtual List<Unit> Units { get; set; }

		[NotMapped] public virtual string Text { get { return GetText(); } set { } }
		[NotMapped] public virtual string? Color { get { return GetHexColor(); } set { } }

		public virtual string GetText()
        {
            return string.Empty;
        }

        public virtual string? GetHexColor()
        {
            return null;
        }

		public bool UnitIsRelated(Unit unit)
		{
			return Units.Contains(unit);
		}

		public bool UnitIsRelated(ulong unitId)
		{
			return Units.Any(u => u.DiscordId == unitId);
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
			builder.HasMany(e => e.Units).WithMany(u => u.SingleDayEvents);
		}
	}
}
