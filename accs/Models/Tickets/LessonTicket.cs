using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket(ulong authorId, ulong channelId) : base(authorId, channelId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService)
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;

            await channel.SendMessageAsync(
                "Вы подали запрос на помощь у инструкторов.\n" +
                "Инструкторский корпус скоро рассмотрит ваш запрос и свяжется с вами."
            );
        }
    }
}
