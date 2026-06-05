using accs.Models.Abstraction;
using accs.Models.Interfaces;
using System.Text.Json;

namespace accs.Models
{
	public class Reward : IEntityWithDiscordRole, IEntityWithFiles
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Color { get; set; } = "#FFFFFF";
		public string Conditions { get; set; } = string.Empty;
		public string Privileges { get; set; } = string.Empty;
		public virtual List<AssignedReward> Assigned { get; set; } = new List<AssignedReward>();
		
		public ulong? DiscordRoleId { get; set; }

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

		public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public string GetImageFolderName()
        {
            return "rewards";
        }
    }
}
