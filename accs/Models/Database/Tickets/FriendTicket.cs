using accs.Database;
using accs.Models.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace accs.Models.Database.Tickets
{
    public class FriendTicket : Ticket
    {
        public FriendTicket(ulong authorId) : base(authorId) { }

        public FriendTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("FriendTicket: channel is null");
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

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет на сотрудничество №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.DarkBlue)
					.AddField("Батальон РХБЗ",
					"Современная организация, сочетающая военные традиции и сплоченную игру коллективом в Squad.")
					.AddField("Основная деятельность",
					"▫️ Участие в ивентах вместе с другими сообществами" +
					"\r\n▫️ Обучение и совместные тренировки" +
					"\r\n▫️ Строевая подготовка и построения по праздникам" +
					"\r\n▫️ Поддерживание онлайна личного состава" +
					"\r\n▫️ Seed проектов")
					.AddField("С чего начать?", "Опишите Ваше предложение.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Закрыть тикет как принятый, доступно администраторам." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно администраторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
        {
            var user = guildProvider.GetGuild().GetUser(AuthorDiscordId);
            if (user != null)
            {
                string friendRoleIdStr = DotNetEnv.Env.GetString("FRIEND_ROLE_ID");
                if (ulong.TryParse(friendRoleIdStr, out ulong friendRoleId))
                {
                    var friendRole = guildProvider.GetGuild().GetRole(friendRoleId);

                    if (friendRole != null)
                    {
                        await user.AddRoleAsync(friendRole);
                    }
                }
            }

			ClosedUserId = closedUserId;
            Status = TicketStatus.Accepted;
            await DeleteChannelAsync(guildProvider);
        }

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
