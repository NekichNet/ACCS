using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
	[Table("CustomEventsWithDoc")]
    public class CustomEventWithDoc : EventWithDoc
    {
        public override string GetHexColor()
        {
            return "#AAAAAA";
        }

        public override string GetText()
        {
            return "Текст ивента";
        }
    }
}
