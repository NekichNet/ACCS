using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
	[Table("CustomEvents")]
    public class CustomEvent : SingleDayEvent
    {
        public override string GetHexColor()
        {
            return "#BBBBBB";
        }

        public override string GetText()
        {
            return "Текст ивента";
        }
    }
}
