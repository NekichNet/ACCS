using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Tickets
{
    public class InviteTicket : Ticket
    {
        public InviteTicket (ulong authorId) : base(authorId) { }

        public InviteTicket()
        {
        }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("InviteTicket: channel is null");
			else
                await channel.SendMessageAsync(
                "Добро пожаловать! Оставьте заявку на вступление в клан. " +
                "Военная полиция скоро свяжется с вами."
            );
        }


        public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
            SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);

            List<Post> shooterPosts = db.Posts.Where(p => p.Name == "Стрелок").ToList();

            if (!shooterPosts.Any())
            {
                await channel.SendMessageAsync("Ошибка: не найдено ни одной должности 'Стрелок'.");
                return;
            }

            var menu = new SelectMenuBuilder() 
                .WithCustomId($"invite-select-{Id}")
                .WithPlaceholder("Взвод")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (Post post in shooterPosts)
                menu.AddOption(post.GetFullName(), post.Id.ToString(), post.Units.Count + " человек");

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await channel.SendMessageAsync(
                "Выберите взвод, за которым будет закреплён рекрут:",
                components: builder.Build()
            );
        }


        public async Task AcceptanceHandler(int selectedPostId, IGuildProviderService guildProvider, AppDbContext db, ILogService logService)
        {
            var channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);          

            // назначаем должность стрелка
            var post = await db.Posts.FindAsync(selectedPostId);

            if (post == null)
            {
                await channel.SendMessageAsync($"Ошибка: выбранная должность стрелка с Id {selectedPostId} не найдена!");
                await logService.WriteAsync($"Выбранная должность стрелка с Id {selectedPostId} не найдена!", LoggingLevel.Error);
                return;
            }

            // выдаём звание рекрута
            var recruitRank = await db.Ranks.FindAsync(1);

			if (recruitRank == null)
			{
				await channel.SendMessageAsync("Ошибка: звание рекрута не найдено!");
				await logService.WriteAsync($"Звание рекрута не найдено!", LoggingLevel.Error);
				return;
			}

            await guildProvider.GetGuild().GetUser(AuthorDiscordId).ModifyAsync(u => u.Nickname = "[Р] " + u.Nickname);

			var unit = new Unit
            {
                DiscordId = AuthorDiscordId,
                Nickname = guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName,
                Rank = recruitRank,
                Posts = new List<Post> { post }
            };

            await db.Units.AddAsync(unit);
            Status = TicketStatus.Accepted;

            await db.SaveChangesAsync();
            await DeleteChannelAsync(guildProvider);
        }

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
            admins.AddRange(db.Posts.Where(p => p.Subdivision != null).Where(p => p.Subdivision.Id == 1));
			return admins;
		}
	}
}
