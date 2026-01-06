namespace accs.Models
{
	public class RewardType
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public List<Reward> Rewards { get; set; }
	}
}
