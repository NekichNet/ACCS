using Business.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace Business.Models.SingleDayEvents
{
    [Table("UnitDismissingEvents")]
    public class UnitDismissingEvent : EventWithDoc
    {
        public override string GetText()
        {
			return $"Уволен";
        }

        public override string GetHexColor()
        {
            return "#994444";
        }
    }
}
