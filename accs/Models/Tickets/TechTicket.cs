using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class TechTicket : Ticket
    {
        public TechTicket(ulong authorId, ulong channelId) : base(authorId, channelId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService)
        {
            SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
            if (channel == null)
                await logService.WriteAsync("TechTicket: channel is null");
            else
                await channel.SendMessageAsync(
                    "Вы обратились в техническую поддержку клана.\n" +
                    "Служба Связи скоро ответит на ваш запрос."
                );
        }

    }
}
