using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class DonationTicket : Ticket
    {
        public DonationTicket(ulong authorId) : base(authorId) { }

        public DonationTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("DonationTicket: channel is null");
			else
			{
				List<Post> adminPosts = await GetAdminsAsync(db);
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

				string requisites = DotNetEnv.Env.GetString("DONATION_REQUISITES",
				"Реквизиты для пожертвований не найдены.");

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет на поддержку клана №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.Gold)
					.AddField("Реквизиты", requisites)
					.AddField("Благодарность", "Спасибо за желание поддержать клан!")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Закрыть тикет как решённый, доступно администраторам." +
					"\r\n***/ticket refuse*** — Закрыть тикет как не решённый, доступно администраторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
        {
            var user = guildProvider.GetGuild().GetUser(AuthorDiscordId);
            if (user != null)
            {
                string friendRoleIdStr = DotNetEnv.Env.GetString("FRIEND_ROLE_ID");
                if (ulong.TryParse(friendRoleIdStr, out ulong friendRoleId))
                {

                    var friendRole = guildProvider.GetGuild().GetRole(friendRoleId);

                    if (friendRole != null)
                    {
                        await user.AddRoleAsync(friendRole);
                    }
                }
            }

			ClosedUserId = closedUserId;
            Status = TicketStatus.Accepted;
            await DeleteChannelAsync(guildProvider);
        }

		public async override Task<List<Post>> GetAdminsAsync(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
