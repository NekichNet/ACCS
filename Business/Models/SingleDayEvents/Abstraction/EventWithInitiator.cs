using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Business.Models.SingleDayEvents.Abstraction
{
	[Table("EventsWithInitiator")]
	public abstract class EventWithInitiator : SingleDayEvent
    {
		public ulong InitiatorId { get; set; }
		[JsonIgnore] public virtual Unit Initiator { get; set; }
	}

	public class EventWithInitiatorConfiguration : IEntityTypeConfiguration<EventWithInitiator>
	{
		public void Configure(EntityTypeBuilder<EventWithInitiator> builder)
		{
			builder.HasOne(e => e.Initiator).WithMany().HasForeignKey(e => e.InitiatorId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
