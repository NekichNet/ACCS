using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket()
        {
        }

        public LessonTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("LessonTicket: channel is null", LoggingLevel.Error);
			else
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет инструкторам №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkOrange)
					.AddField("Инструкторский корпус РХБЗ",
					"Подразделение нашего сообщества, состоящее из опытных игроков, которые" +
					" готовы помочь с освоением в игре или развитием отдельных навыков")
					.AddField("С чего начать?", "Напишите Ваш вопрос, касающийся Squad, " +
					"опишите свою проблему или сразу попросите конкретный урок.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Помощь оказана, закрыть тикет, доступно инструкторам." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно инструкторам.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 4));
			return admins;
		}
	}
}
