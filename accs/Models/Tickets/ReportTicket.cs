using accs.Database;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class ReportTicket : Ticket
    {
        public ReportTicket()
        {
        }

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

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 1));
			return admins;
		}
	}
}
