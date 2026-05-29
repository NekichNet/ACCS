using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents.Abstraction
{
    [Table("Events")]
    public abstract class SingleDayEvent
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public virtual List<Unit> Units { get; set; }

        public abstract string GetText();
        public abstract string GetHexColor();
    }
}
