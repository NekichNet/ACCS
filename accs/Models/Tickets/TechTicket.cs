using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class TechTicket : Ticket
    {
        public TechTicket()
        {
        }

        public TechTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("TechTicket: channel is null", LoggingLevel.Error);
			else
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет в техподдержку №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkRed)
					.AddField("С чего начать?",
					"Опишите проблему, с которой Вы столкнулись, или изложите " +
					"Ваше предложение по улучшению технической составляющей нашего сообщества")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять тикет, доступно службе связи." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно службе связи.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
        {
            List<Post> admins = base.GetAdmins(db);
            admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 3));
			return admins;
        }
    }
}
