using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class DonationTicket : Ticket
    {
        public DonationTicket(ulong authorId) : base(authorId) { }

        public DonationTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("DonationTicket: channel is null", LoggingLevel.Error);
			else
			{
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
					"\r\n***/ticket refuse*** — Закрыть тикет как не решённый, доступно администраторам.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db)
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

            Status = TicketStatus.Accepted;
            await DeleteChannelAsync(guildProvider);
        }

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
