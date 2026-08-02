using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
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

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("InviteTicket: channel is null");
			else
            {
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

				foreach (Post post in db.Posts.Where(p => p.Id == 3 || p.Id == 4))
				{
					if (post.DiscordRoleId != null)
					{
						RestRole role = await guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId);
						if (role != null)
							text += role.Mention;
					}
				}

				EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle($"Тикет на вступление №{Id}")
                    .WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
                    .WithColor(Color.DarkGreen)
                    .AddField("Батальон РХБЗ",
					"Современная организация, сочетающая военные традиции и сплоченную игру коллективом в Squad.")
                    .AddField("Основная деятельность",
					"▫️ Участие в ивентах вместе с другими сообществами" +
					"\r\n▫️ Обучение и совместные тренировки" +
                    "\r\n▫️ Строевая подготовка и построения по праздникам" +
					"\r\n▫️ Поддерживание онлайна личного состава" +
					"\r\n▫️ Seed проектов")
                    .AddField("Вам сейчас необходимо", "[Заполнить анкету для вступления](https://forms.gle/bLPB7AGxecPSWfR2A)")
                    .AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять в клан, доступно ВП." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно ВП." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.")
                    .WithImageUrl("https://c.tenor.com/mCr1ijrLsyUAAAAd/tenor.gif");
                await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
            }
        }

        public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
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


        public async Task AcceptanceHandler(int selectedPostId, IGuildProviderService guildProvider, AppDbContext db, ILogger log, ulong closedUserId)
        {
            var channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			// назначаем должность стрелка
			var post = await db.Posts.FindAsync(selectedPostId);
            
            if (post == null)
            {
                await channel.SendMessageAsync($"Ошибка: выбранная должность стрелка с Id {selectedPostId} не найдена!");
				log.LogError($"Выбранная должность стрелка с Id {selectedPostId} не найдена!");
                return;
            }
			// выдаём звание рекрута
			var recruitRank = await db.Ranks.FindAsync(1);

			if (recruitRank == null)
			{
				await channel.SendMessageAsync("Ошибка: звание рекрута не найдено!");
				log.LogError($"Звание рекрута не найдено!");
				return;
			}

			SocketGuildUser author = guildProvider.GetGuild().GetUser(AuthorDiscordId);
            string nickname = author.DisplayName;
			//await author.ModifyAsync(u => u.Nickname = "[Р] " + nickname);
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

            ClosedUserId = closedUserId;
			Status = TicketStatus.Accepted;

			await db.SaveChangesAsync();
            await DeleteChannelAsync(guildProvider);

            await post.NotifyOnAssignAsync(guildProvider.GetGuild(), db, unit);
		}

		public override List<Post> GetAdmins(AppDbContext db)
		{
            List<Post> admins = db.Posts
                .Where(p => p.Name.Contains("омандир"))
                .ToList();
            foreach (Post admin in admins)
            {
                admins.AddRange(admin.GetAllHeadsRecursive());
            }
			return admins.ToHashSet().ToList();
		}
	}
}
