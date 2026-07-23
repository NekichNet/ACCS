using Business.Database;
using Business.Models.Enums;
using Business.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Business.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket()
        {
        }

        public LessonTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("LessonTicket: channel is null");
			else
			{
				List<Post> adminPosts = GetAdmins(db);
				string text = "";
				SocketGuildUser authorUser = guildProvider.GetGuild().GetUser(AuthorDiscordId);
				if (authorUser != null)
				{
					text += authorUser.Mention;
				}
				else
				{
					log.LogError($"Ticket: authorUser with Id {AuthorDiscordId} is null");
				}

				foreach (Post post in adminPosts)
				{
					if (post.DiscordRoleId != null)
					{
						RestRole role = await guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId);
						if (role != null)
							text += role.Mention;
					}
				}

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
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно инструкторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 4));
			return admins;
		}
	}
}
