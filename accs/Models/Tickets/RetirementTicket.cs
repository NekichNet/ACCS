using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Tickets
{
    public class RetirementTicket : Ticket
    {
        public RetirementTicket(ulong authorId) : base(authorId) { }

        public RetirementTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("RetirementTicket: channel is null", LoggingLevel.Error);
			else
			{
				Unit? unit = await db.Units.FindAsync(AuthorDiscordId);

				if (unit == null)
				{
					await logService.WriteAsync("RetirementTicket: unit is null", LoggingLevel.Error);
					await channel.SendMessageAsync("Ошибка: автор тикета не найден в системе!");
					return;
				}

				bool inRetirement = unit.UnitStatuses.Any(us =>
					us.Status.Type == StatusType.Retirement && !us.IsCompleted());

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет отставки №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(inRetirement ? Color.Teal : Color.DarkGrey)
					.AddField("Вы решили выйти " + (inRetirement ? "из отставки" : "в отставку"),
					"Можете написать причину и/или сроки.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Помощь оказана, закрыть тикет, доступно администрации." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно администрации.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
            Unit? unit = await db.Units.FindAsync(AuthorDiscordId);
            var channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);

            if (unit == null)
            {
                await channel.SendMessageAsync("Ошибка: боец не найден.");
                return;
            }

            // поиск активного статуса Retirement
            var activeRetirement = unit.UnitStatuses
                .FirstOrDefault(us =>
                    us.Status.Type == StatusType.Retirement &&
                    !us.IsCompleted()
                );

            if (activeRetirement == null)
            {
				// еще не в отставке -> в отставку
                Status? retirementStatus = await db.Statuses.FindAsync(StatusType.Retirement);
                if (retirementStatus == null)
                {
                    await channel.SendMessageAsync("Ошибка: статус Retirement не найден.");
                    return;
                }

                var unitStatus = new UnitStatus
                {
                    Unit = unit,
                    Status = retirementStatus,
                    StartDate = DateTime.UtcNow
                };

                foreach (Post post in unit.Posts)
                {
					List<IRole> roles = new List<IRole>();
					if (post.DiscordRoleId != null)
						roles.Add(await guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId));
					Subdivision? subdiv = post.Subdivision;
					while (subdiv != null)
					{
						if (subdiv.DiscordRoleId != null)
							roles.Add(await guildProvider.GetGuild().GetRoleAsync((ulong)subdiv.DiscordRoleId));
						subdiv = subdiv.Head;
					}

					await guildProvider.GetGuild().GetUser(AuthorDiscordId).RemoveRolesAsync(roles);
				}

				unit.Posts.Clear();
				await db.UnitStatuses.AddAsync(unitStatus);
                

                await channel.SendMessageAsync(
                    "Вы успешно отправлены в отставку. Все ваши должности сняты."
                );

                Status = TicketStatus.Accepted;
                await DeleteChannelAsync(guildProvider);
				await db.SaveChangesAsync();
			}
            else
            {
				// уже в отставке -> показываем меню выбора должностей
				List<Post> allPosts = await db.Posts.Include(p => p.Units).ToListAsync();

				var menu = new SelectMenuBuilder()
					.WithCustomId($"retirement-select-{Id}")
					.WithPlaceholder("Должности")
					.WithMinValues(1)
					.WithMaxValues(allPosts.Count);

				foreach (Post post in allPosts)
				{
					menu.AddOption(post.GetFullName(), post.Id.ToString(), post.Units.Count + " человек");
				}

				var builder = new ComponentBuilder().WithSelectMenu(menu);

				await channel.SendMessageAsync(
					"Выберите должности, которые хотите получить после возвращения из отставки:",
					components: builder.Build()
				);
			}
        }

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
