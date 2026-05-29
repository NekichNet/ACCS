using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.Statuses.Abstraction
{
	[Table("UnitStates")]
    public abstract class UnitState
    {
		public int Id { get; set; }
		public DateTime Start { get; set; } = DateTime.UtcNow;
		public DateTime? End { get; set; }
		public ulong UnitId { get; set; }
		public virtual Unit Unit { get; set; }

		public bool IsActive()
		{
			return End != null ? DateTime.UtcNow < End : true;
		}

		public void Terminate()
		{
			End = DateTime.UtcNow;
		}
	}
}
