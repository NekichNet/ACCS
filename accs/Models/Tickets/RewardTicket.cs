using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
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
				List<Post> adminPosts = GetAdmins(db);
				string text = "";
				SocketGuildUser authorUser = guildProvider.GetGuild().GetUser(AuthorDiscordId);
				if (authorUser != null)
				{
					text += authorUser.Mention;
				}
				else
				{
					await logService.WriteAsync($"Ticket: authorUser with Id {AuthorDiscordId} is null", LoggingLevel.Error);
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
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Id == 23));
			return admins;
		}
	}
}
