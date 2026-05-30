using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents.Abstraction
{
    [Table("EventsWithDocs")]
    public abstract class EventWithDoc : SingleDayEvent
    {
        public int DocId { get; set; }
        public virtual Doc Doc { get; set; }
    }
}