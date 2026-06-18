using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
	[Table("UnitRegistrationEvents")]
    public class UnitRegistrationEvent : EventWithInitiator
    {
        public override string GetHexColor()
        {
            return "#44FF77";
        }

        public override string GetText()
        {
            return "Зарегистрирован в системе";
        }
    }
}
