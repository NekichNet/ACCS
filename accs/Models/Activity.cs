using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
    [EntityTypeConfiguration(typeof(ActivityConfiguration))]
    public class Activity
	{
		public ulong UnitId { get; set; }
		public virtual Unit Unit { get; set; }
		public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
	}
}
