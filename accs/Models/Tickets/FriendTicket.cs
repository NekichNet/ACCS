using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class FriendTicket : Ticket
    {
        public FriendTicket(ulong authorId) : base(authorId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("FriendTicket: channel is null");
			else
				await channel.SendMessageAsync(
               "Вы подали заявку на сотрудничество с кланом.\n" +
                "Командир РХБЗ или его заместитель скоро рассмотрят ваш запрос."
            );
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
