using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents.Abstraction
{
	[Table("EventsWithDocs")]
    public abstract class EventWithDoc : SingleDayEvent
    {
        public int DocId { get; set; }
        [JsonIgnore] public virtual Doc Doc { get; set; }
    }

	public class EventWithDocConfiguration : IEntityTypeConfiguration<EventWithDoc>
	{
		public void Configure(EntityTypeBuilder<EventWithDoc> builder)
		{
			builder.HasOne(e => e.Doc).WithMany().HasForeignKey(e => e.DocId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}