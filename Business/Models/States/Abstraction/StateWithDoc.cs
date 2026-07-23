using Business.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Business.Models.States.Abstraction
{
	[Table("States")]
	public abstract class StateWithDoc : UnitState
	{
		public int? DocId { get; set; }
		[JsonIgnore] public virtual Doc? Doc { get; set; }
	}

	public class StateWithDocConfiguration : IEntityTypeConfiguration<StateWithDoc>
	{
		public void Configure(EntityTypeBuilder<StateWithDoc> builder)
		{
			builder.HasOne(s => s.Doc).WithMany().HasForeignKey(e => e.DocId).OnDelete(DeleteBehavior.NoAction);
		}
	}
}
