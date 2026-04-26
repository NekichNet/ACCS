namespace accs.Models.Database
{
	public class Reward
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public ulong DiscordRoleId { get; set; }
		public string? ImagePath { get; set; } // Путь к картинке на диске
		public virtual List<AssignedReward> Assigned { get; set; } = new List<AssignedReward>();

        public override string ToString()
        {
            return Id.ToString() + " " + Name;
        }
	}
}
