namespace accs.Models
{
	public class Reward
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Conditions { get; set; } = string.Empty;
		public string Privileges { get; set; } = string.Empty;
		public ulong DiscordRoleId { get; set; }
		public string? ImagePath { get; set; } // Путь к картинке на диске
		public virtual List<Unit> Units { get; set; } = new List<Unit>();
	}
}
