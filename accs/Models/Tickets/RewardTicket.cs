using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class RewardTicket : Ticket
    {
		public RewardTicket()
		{
		}

		public RewardTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("RewardTicket: channel is null", LoggingLevel.Error);
			else
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет на представление к награде №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkPurple)
					.AddField("С чего начать?",
					"Напишите никнейм, на какую награду претендуете и скиньте необходимые пруфы.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Закрыть тикет как решённый, доступно администраторам." +
					"\r\n***/ticket refuse*** — Закрыть тикет как не решённый, доступно администраторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
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
