using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents.Abstraction
{
    public abstract class EventWithInitiator : SingleDayEvent
    {
		public ulong InitiatorId { get; set; }
		[ForeignKey("InitiatorId")]
		[JsonIgnore] public virtual Unit Initiator { get; set; }
	}
}
