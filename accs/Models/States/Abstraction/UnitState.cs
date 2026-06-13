using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models.Statuses.Abstraction
{
	public abstract class UnitState
    {
		public int Id { get; set; }
		public DateTime Start { get; set; } = DateTime.UtcNow;
		public DateTime? End { get; set; } = null;
		public ulong UnitId { get; set; }
		[JsonIgnore] public virtual Unit Unit { get; set; }

		public bool IsActive(DateTime? dateTime = null)
		{ // "dateTime < End" возможно будет не работать без отдельной проверки End на null
			dateTime = dateTime ?? DateTime.UtcNow;
			return Start < dateTime && (End == null || dateTime < End);
		}

		public void Terminate()
		{
			End = DateTime.UtcNow;
		}

		public virtual string GetText()
		{
			return string.Empty;
		}

		public virtual string? GetHexColor()
		{
			return null;
		}
		
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}

	public class UnitStateConfiguration : IEntityTypeConfiguration<UnitState>
	{
		public void Configure(EntityTypeBuilder<UnitState> builder)
		{
			builder.HasOne(us => us.Unit).WithMany(u => u.UnitStates).HasForeignKey(us => us.UnitId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
