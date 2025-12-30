using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
{
	public class Unit
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public string DiscordId { get; set; }
		public string Nickname { get; set; }
		public string? SteamId { get; set; }
		public Rank Rank { get; set; }
		public List<Doc> OwnDocs { get; set; }
		public List<Doc> AssignedDocs { get; set; }
		public List<UnitPost> Posts { get; set; } = new List<UnitPost>();
		public List<Reward> Rewards { get; set; } = new List<Reward>();
		public List<Activity> Activities { get; set; } = new List<Activity>();
	}
}
