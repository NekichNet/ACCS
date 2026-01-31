using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class ReportTicket : Ticket
    {
        public ReportTicket()
        {
        }

        public ReportTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("ReportTicket: channel is null", LoggingLevel.Error);
			else
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Жалоба №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkBlue)
					.AddField("С чего начать?", "Изложите Вашу жалобу, не опускайте подробности.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять тикет, доступно ВП." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно ВП.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 1));
			return admins;
		}
	}
}
