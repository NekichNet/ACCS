using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
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
					.WithTitle($"Тикет в техподдержку №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkRed)
					.AddField("С чего начать?",
					"Опишите проблему, с которой Вы столкнулись, или изложите " +
					"Ваше предложение по улучшению технической составляющей нашего сообщества")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять тикет, доступно службе связи." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно службе связи." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
        {
            List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 3));
			return admins;
        }
    }
}
