using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class TechTicket : Ticket
    {
        public TechTicket(SocketGuild guild, ulong authorId, ulong channelId) : base(guild, authorId, channelId)
        {

        }

        public override async Task SendWelcomeMessage()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;

            await channel.SendMessageAsync(
                "Вы обратились в техническую поддержку клана. " +
                "Служба Связи скоро ответит на ваш запрос."
            );
        }

    }
}
