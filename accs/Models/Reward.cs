using accs.Models.Interfaces;

namespace accs.Models
{
	public class Reward : IEntityWithDiscordRole
	{
		public int Id { get; set; }
		public string Conditions { get; set; } = string.Empty;
		public string Privileges { get; set; } = string.Empty;
		public string? ImagePath { get; set; } // Путь к картинке на диске
		public virtual List<AssignedReward> Assigned { get; set; } = new List<AssignedReward>();
		public string Color { get; set; } = "#FFFFFF";
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }

		public override string ToString()
        {
            return Id.ToString() + " " + Name;
        }

        public void UpdateRole()
        {
            if (DiscordRoleId != null)
            {
                // TODO: Send request to discord-bot api
            }
        }

        public void CheckRoleOnUser(ulong unitId)
        {
            if (DiscordRoleId != null)
            {
                // TODO: Send request to discord-bot api
            }
        }
    }
}
