using Business.Database;
using Business.Models.Enums;
using Business.Models.Statuses.Abstraction;
using Business.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Business.Models.Tickets
{
    public class RetirementTicket : Ticket
    {
        public RetirementTicket(ulong authorId) : base(authorId) { }

        public RetirementTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("RetirementTicket: channel is null");
			else
			{
				List<Post> adminPosts = GetAdmins(db);
				string text = "";
				SocketGuildUser authorUser = guildProvider.GetGuild().GetUser(AuthorDiscordId);
				if (authorUser != null)
				{
					text += authorUser.Mention;
				}
				else
				{
					log.LogError($"Ticket: authorUser with Id {AuthorDiscordId} is null");
				}

				foreach (Post post in adminPosts)
				{
					if (post.DiscordRoleId != null)
					{
						RestRole role = await guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId);
						if (role != null)
							text += role.Mention;
					}
				}

				Unit? unit = await db.Units.FindAsync(AuthorDiscordId);

				if (unit == null)
				{
					log.LogError("RetirementTicket: unit is null");
					await channel.SendMessageAsync("Ошибка: автор тикета не найден в системе!");
					return;
				}

				bool inRetirement = unit.UnitStates.Any(us =>
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
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно администрации." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
        {
            Unit? unit = await db.Units.FindAsync(AuthorDiscordId);
            var channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);

            if (unit == null)
            {
                await channel.SendMessageAsync("Ошибка: боец не найден.");
                return;
            }

            // поиск активного статуса Retirement
            var activeRetirement = unit.UnitStates
                .FirstOrDefault(us =>
                    us.Status.Type == StatusType.Retirement &&
                    !us.IsCompleted()
                );

            if (activeRetirement == null)
            {
				// еще не в отставке -> в отставку
                UnitState? retirementStatus = await db.UnitStates.FindAsync(StatusType.Retirement);
                if (retirementStatus == null)
                {
                    await channel.SendMessageAsync("Ошибка: статус Retirement не найден.");
                    return;
                }

                var unitStatus = new UnitStatus
                {
                    Unit = unit,
                    Status = retirementStatus,
                    Start = DateTime.UtcNow
				};

				SocketGuildUser user = guildProvider.GetGuild().GetUser(AuthorDiscordId);

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

					await user.RemoveRolesAsync(roles);
				}

				unit.Posts.Clear();
				await db.UnitStatuses.AddAsync(unitStatus);

				if (unit.Rank.DiscordRoleId != null)
					await user.RemoveRoleAsync((ulong)unit.Rank.DiscordRoleId);

				await channel.SendMessageAsync(
                    "Вы успешно отправлены в отставку. Все ваши должности сняты."
                );

                Status = TicketStatus.Accepted;
				ClosedUserId = closedUserId;
                await DeleteChannelAsync(guildProvider);
				await db.SaveChangesAsync();
				unitStatus.SetRole(guildProvider);
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
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
