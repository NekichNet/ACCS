using accs.Database;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class ReportTicket : Ticket
    {
        public ReportTicket(ulong authorId) : base(authorId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("ReportTicket: channel is null");
			else
				await channel.SendMessageAsync(
                "Вы обратились в отдел жалоб клана.\n" +
                "Военная Полиция скоро рассмотрит ваш запрос и свяжется с вами."
            );
        }
    }
}
