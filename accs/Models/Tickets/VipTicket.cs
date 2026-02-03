using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class VipTicket : Ticket
	{
        public VipTicket()
        {
        }

        public VipTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("VipTicket: channel is null", LoggingLevel.Error);
			else
			{
				string requisites = DotNetEnv.Env.GetString("DONATION_REQUISITES",
				"Реквизиты для пожертвований не найдены.");

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Тикет на получение Vip сервера №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.Blue)
					.AddField("Если Вы всё ещё не привязали Steam ID",
					"Воспользуйтесь командой `/steam` прямо сейчас. Также, сообщите свой Steam ID в этом тикете.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Закрыть тикет как решённый, доступно администраторам." +
					"\r\n***/ticket refuse*** — Закрыть тикет как не решённый, доступно администраторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build());
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
