using accs.Database;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket(ulong authorId) : base(authorId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("LessonTicket: channel is null");
			else
				await channel.SendMessageAsync(
                "Вы подали запрос на помощь у инструкторов.\n" +
                "Инструкторский корпус скоро рассмотрит ваш запрос и свяжется с вами."
            );
        }
    }
}
