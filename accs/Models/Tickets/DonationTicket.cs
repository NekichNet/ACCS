using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class DonationTicket : Ticket
    {
        public DonationTicket(ulong authorId) : base(authorId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("DonationTicket: channel is null");
			else
            {
				string requisites = DotNetEnv.Env.GetString("DONATION_REQUISITES",
				"Реквизиты для пожертвований не найдены.");

				await channel.SendMessageAsync(
					"Спасибо за желание поддержать клан!\n" +
					"Вот реквизиты для пожертвования:\n\n" +
					requisites
				);
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
    }
}
