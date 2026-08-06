using accs.Database;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class SuggestionTicket : Ticket
	{
		public SuggestionTicket()
		{
		}

		public SuggestionTicket(ulong authorId) : base(authorId) { }

		public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogger<Ticket> log, AppDbContext db)
		{
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				log.LogError("SuggestionTicket: channel is null");
			else
			{
				List<Post> adminPosts = await GetAdminsAsync(db);
				string text = "";
				SocketGuildUser authorUser = guildProvider.GetGuild().GetUser(AuthorDiscordId);
				if (authorUser != null)
				{
					text += authorUser.Mention;
				}
				else
				{
					log.LogError($"SuggestionTicket: authorUser with Id {AuthorDiscordId} is null");
				}

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle($"Предложение №{Id}")
					.WithDescription("Автор: " + guildProvider.GetGuild().GetUser(AuthorDiscordId).DisplayName)
					.WithColor(Color.Teal)
					.AddField("С чего начать?",
					"Подробно изложите Вашу идею по улучшению жизни клана и пинганите руководителя соответствующего направления.")
					.AddField("Команды",
					"***/ticket cancel*** — Отменить тикет, доступно автору." +
					"\r\n***/ticket accept*** — Принять тикет, доступно администраторам." +
					"\r\n***/ticket refuse*** — Отказать в тикете, доступно администраторам." +
					"\r\n***/ticket voice*** — Создать приватный голосовой канал, доступно всем.");
				await channel.SendMessageAsync(embed: embed.Build(), text: text, allowedMentions: AllowedMentions.All);
			}
		}

		public async override Task<List<Post>> GetAdminsAsync(AppDbContext db)
		{
			List<Post> admins = new List<Post>();
			admins.AddRange(db.Posts.Where(
				p => p.Id < 7
				|| (p.Id > 15 && p.Id < 22)
				|| p.Id == 23
				|| (p.Id > 27 && p.Id < 30)));
			return admins;
		}
	}
}
