using accs.Database;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class LessonTicket : Ticket
    {
        public LessonTicket()
        {
        }

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

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 4));
			return admins;
		}
	}
}
