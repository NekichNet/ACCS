using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents
{
    [Table("UnitRegistrationEvents")]
    public class UnitRegistrationEvent : EventWithInitiator
    {
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
