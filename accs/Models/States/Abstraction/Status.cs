using accs.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace accs.Models.States.Abstraction
{
    [Table("Statuses")]
    public abstract class Status : StateWithDoc, IEntityWithDiscordRole
    {
        public string Color { get; set; }
        public string Name { get; set; }
        public ulong? DiscordRoleId { get; set; }

		public override string ToString()
		{
			return JsonSerializer.Serialize(this);
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

		public abstract int GetIndex();
	}
}
