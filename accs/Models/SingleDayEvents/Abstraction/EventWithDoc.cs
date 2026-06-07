using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents.Abstraction
{
    [Table("EventsWithDocs")]
    public abstract class EventWithDoc : SingleDayEvent
    {
        public int DocId { get; set; }
        [ForeignKey("DocId")]
        [JsonIgnore] public virtual Doc Doc { get; set; }
    }
}