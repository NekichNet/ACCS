using accs.Database;
using accs.Models.Database;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace accs.Models.Database.Tickets
{
    public class ReportTicket : Ticket
    {
        public ReportTicket()
        {
        }

        public ReportTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("ReportTicket: channel is null");
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
					.WithTitle($"Жалоба №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkBlue)
					.AddField("С чего начать?", "Изложите Вашу жалобу, не опускайте подробности.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять тикет, доступно ВП." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно ВП." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 1));
			return admins;
		}
	}
}
