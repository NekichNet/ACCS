using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket(SocketGuild guild, ulong authorId, ulong channelId) : base(guild, authorId, channelId)
        {
        }

        public override async Task SendWelcomeMessage()
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
