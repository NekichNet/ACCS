using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
    [EntityTypeConfiguration(typeof(ActivityConfiguration))]
    public class Activity
	{
		public Unit Unit { get; set; }
		public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
		public bool Confirmed { get; set; } = false;
	}
}
