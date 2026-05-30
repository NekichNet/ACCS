using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
    [Table("UnitRegistrationEvents")]
    public class UnitRegistrationEvent : SingleDayEvent
    {
        public ulong InitiatorId { get; set; }
        public virtual Unit Initiator { get; set; }

        public override string GetHexColor()
        {
            throw new NotImplementedException();
        }

        public override string GetText()
        {
            throw new NotImplementedException();
        }
    }
}
