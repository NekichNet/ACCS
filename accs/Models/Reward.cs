namespace accs.Models
{
	public class Reward
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public string? ImagePath { get; set; } // Путь к картинке на диске
		public Doc? Doc { get; set; } // Документ о награждении
		public List<Unit> Units { get; set; } = new List<Unit>();
	}
}
