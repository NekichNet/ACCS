using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class FriendTicket : Ticket
    {
        public FriendTicket(SocketGuild guild, ulong authorId, ulong channelId
            ) : base(guild, authorId, channelId)
        {
        }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService)
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
            {
                return;
            }
            await channel.SendMessageAsync(
               "Вы подали заявку на сотрудничество с кланом.\n" +
                "Командир РХБЗ или его заместитель скоро рассмотрят ваш запрос."
            );
        }


        public override async Task AcceptAsync()
        {
            var user = _guild.GetUser(AuthorDiscordId);
            if (user != null)
            {
                string friendRoleIdStr = DotNetEnv.Env.GetString("FRIEND_ROLE_ID");
                if (ulong.TryParse(friendRoleIdStr, out ulong friendRoleId))
                {
                    var friendRole = _guild.GetRole(friendRoleId);

                    if (friendRole != null)
                    {
                        await user.AddRoleAsync(friendRole);
                    }
                }
            }

            Status = TicketStatus.Accepted;
            await CloseAsync();
        }
    }
}
