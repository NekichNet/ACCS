using accs.Models.Database.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Database
{
    [EntityTypeConfiguration(typeof(ActivityConfiguration))]
    public class Activity
	{
		public ulong UnitId { get; set; }
		public virtual Unit Unit { get; set; }
		public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public override string ToString()
        {
            return UnitId + " " + Date.ToShortDateString();
        }
	}
}
