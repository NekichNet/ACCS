using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
    [Table("UnitDismissingEvents")]
    public class UnitDismissingEvent : EventWithInitiator
    {
        public override string GetText()
        {
			return $"Уволен бойцом {Initiator.Nickname}";
        }

        public override string GetHexColor()
        {
            return "#994444";
        }
    }
}
