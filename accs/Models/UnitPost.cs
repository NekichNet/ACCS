namespace accs.Models
{
	public class UnitPost
	{
		public int Id { get; set; }
		public Unit Unit { get; set; }
		public Post Post { get; set; }
		public DateTime Start { get; set; } = DateTime.UtcNow;
		public DateTime End { get; set; }
	}
}
