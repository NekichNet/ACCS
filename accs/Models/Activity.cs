namespace accs.Models
{
	public class Activity
	{
		public int Id { get; set; }
		public Unit Unit { get; set; }
		public DateTime Date { get; set; } = DateTime.UtcNow;
	}
}
