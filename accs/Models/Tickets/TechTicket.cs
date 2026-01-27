using accs.Database;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class TechTicket : Ticket
    {
        public TechTicket()
        {
        }

        public TechTicket(ulong authorId) : base(authorId) { }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
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

        public override List<Post> GetAdmins(AppDbContext db)
        {
            List<Post> admins = base.GetAdmins(db);
            admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 3));
			return admins;
        }
    }
}
