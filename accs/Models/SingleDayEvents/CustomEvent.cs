using accs.Models.SingleDayEvents.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
    [Table("CustomEvents")]
    public class CustomEvent : SingleDayEvent
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
