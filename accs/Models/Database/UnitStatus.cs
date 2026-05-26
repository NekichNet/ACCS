using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Database
{
    public class UnitStatus // Для любых временных статусов
    {
        public int Id { get; set; }
		public virtual Unit Unit { get; set; }
		public virtual Status Status { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }

        public bool IsCompleted()
        {
            return EndDate == null ? false : EndDate < DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public void SetRole(IGuildProviderService guildProvider)
        {
            if (Status.DiscordRoleId != null)
            {
				SocketGuild guild = guildProvider.GetGuild();
                if (guild != null)
                {
                    SocketGuildUser user = guild.GetUser(Unit.DiscordId);
                    user.AddRoleAsync((ulong)Status.DiscordRoleId);
                }
			}
        }

        public void RemoveRole(IGuildProviderService guildProvider)
        {
			if (Status.DiscordRoleId != null)
			{
				SocketGuild guild = guildProvider.GetGuild();
				if (guild != null)
				{
					SocketGuildUser user = guild.GetUser(Unit.DiscordId);
					user.RemoveRoleAsync((ulong)Status.DiscordRoleId);
				}
			}
		}
    }
}
