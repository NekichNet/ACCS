using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class ReportTicket : Ticket
    {
        public ReportTicket(SocketGuild guild, ulong authorId, ulong channelId) : base(guild, authorId, channelId)
        {
        }

        public override async Task SendWelcomeMessage()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;

            await channel.SendMessageAsync(
                "Вы обратились в отдел жалоб клана.\n" +
                "Военная Полиция скоро рассмотрит ваш запрос и свяжется с вами."
            );
        }
    }
}
