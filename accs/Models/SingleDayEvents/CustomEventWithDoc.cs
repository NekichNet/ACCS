using accs.Models.SingleDayEvents.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
	[Table("CustomEventsWithDoc")]
    public class CustomEventWithDoc : EventWithDoc
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
