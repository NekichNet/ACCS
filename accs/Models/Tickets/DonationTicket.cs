using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class DonationTicket : Ticket
    {
        public DonationTicket(SocketGuild guild, ulong authorId, ulong channelId) : base(guild, authorId, channelId)
        {
        }

        public override async Task SendWelcomeMessageAsync()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;

            string requisites = DotNetEnv.Env.GetString("DONATION_REQUISITES",
                "Реквизиты для пожертвований не найдены.");

            await channel.SendMessageAsync(
                "Спасибо за желание поддержать клан!\n" +
                "Вот реквизиты для пожертвования:\n\n" +
                requisites
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
