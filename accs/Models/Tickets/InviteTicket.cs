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

			SocketGuildUser author = guildProvider.GetGuild().GetUser(AuthorDiscordId);
            string nickname = author.DisplayName;
			await author.ModifyAsync(u => u.Nickname = "[Р] " + nickname);
			var unit = new Unit
            {
                DiscordId = AuthorDiscordId,
                Nickname = author.DisplayName,
                Rank = recruitRank,
                Joined = DateTime.UtcNow,
                Posts = new List<Post> { post }
            };

            List<IRole> roles = new List<IRole>();
            if (recruitRank.DiscordRoleId != null)
                roles.Add(await guildProvider.GetGuild().GetRoleAsync((ulong)recruitRank.DiscordRoleId));
            if (post.DiscordRoleId != null)
                roles.Add(await guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId));
			Subdivision? subdiv = post.Subdivision;
			while (subdiv != null)
            {
				if (subdiv.DiscordRoleId != null)
					roles.Add(await guildProvider.GetGuild().GetRoleAsync((ulong)subdiv.DiscordRoleId));
                subdiv = subdiv.Head;
			}

			await author.AddRolesAsync(roles);
			string friendRoleIdStr = DotNetEnv.Env.GetString("FRIEND_ROLE_ID");
			if (ulong.TryParse(friendRoleIdStr, out ulong friendRoleId))
				if (author.Roles.Any(r => r.Id == friendRoleId))
                    await author.RemoveRoleAsync(friendRoleId);
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
