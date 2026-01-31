using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class FriendTicket : Ticket
    {
        public FriendTicket(ulong authorId) : base(authorId) { }

        public FriendTicket()
        {
        }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("FriendTicket: channel is null", LoggingLevel.Error);
			else
			{
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
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно администраторам.");
				await channel.SendMessageAsync(embed: embed.Build());
			}
		}

		public override async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db)
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

            Status = TicketStatus.Accepted;
            await DeleteChannelAsync(guildProvider);
        }

		public override List<Post> GetAdmins(AppDbContext db)
		{
			List<Post> admins = base.GetAdmins(db);
			admins.AddRange(db.Posts.Where(p => p.Id < 3));
			return admins;
		}
	}
}
