using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
	[InChannels("TICKET_CHANNEL_ID")]
	public class TicketMessageHandler : InteractionModuleBase<SocketInteractionContext>
	{
		private DiscordSocketClient _client;
		private ILogService _logService;
		private SocketTextChannel _channel;

		public TicketMessageHandler(DiscordSocketClient client, ILogService logService)
		{
			_client = client;
			_logService = logService;

			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (ulong.TryParse(channelIdString, out channelId)) { throw _logService.ExceptionAsync("Cannot parse ticket channel id!").Result; }

			_channel = _client.GetChannel(channelId) as SocketTextChannel;
			if (_channel == null) { throw _logService.ExceptionAsync("Ticket channel is null!").Result; }

			_client.Ready += InitTicketMessage;
		}

		[HasPermission(PermissionType.Administrator)]
		[SlashCommand("init-ticket-message", "Инициализация сообщения для тикетов.")]
		public async Task InitTicketMessage()
		{
			await _channel.DeleteMessagesAsync(await _channel.GetMessagesAsync().FlattenAsync());

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Тикеты")
				.WithDescription("Тикет — текстовое обращение, которое видят только Вы и нужная Вам служба.")
				.WithFooter(footer => footer.Text = "Чтобы сформировать тикет, нажмите на соответствующую кнопку ниже:")
				.WithAuthor(Context.Client.CurrentUser)
				.WithColor(Color.DarkGreen);

			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Вступить", "invite-button", ButtonStyle.Success, new Emoji("U+1FA96"), row: 0)
				.WithButton("Сотрудничать", "friend-button", ButtonStyle.Primary, new Emoji("U+1F91D"), row: 0)
				.WithButton("Инструктор", "lesson-button", ButtonStyle.Primary, new Emoji("U+1F9D1"), row: 1)
				.WithButton("Техподдержка", "tech-button", ButtonStyle.Secondary, new Emoji("U+1F198"), row: 1)
				.WithButton("Пожаловаться", "report-button", ButtonStyle.Danger, new Emoji("U+1F46E"), row: 2)
				.WithButton("Отставка", "retirement-button", ButtonStyle.Danger, new Emoji("U+1FAE1"), row: 2)
				.WithButton("Спонсировать", "donation-button", ButtonStyle.Premium, new Emoji("U+1F4B5"), row: 3);

			await _channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
		}

		[IsUnit(false)]
		[ComponentInteraction("invite-button")]
		public async Task CreateInviteTicket()
		{
			throw new NotImplementedException();
		}

		[IsUnit(false)]
		[ComponentInteraction("friend-button")]
		public async Task CreateFriendTicket()
		{
			throw new NotImplementedException();
		}

		[ComponentInteraction("lesson-button")]
		public async Task CreateLessonTicket()
		{
			throw new NotImplementedException();
		}

		[ComponentInteraction("tech-button")]
		public async Task CreateTechTicket()
		{
			throw new NotImplementedException();
		}

		[ComponentInteraction("report-button")]
		public async Task CreateReportTicket()
		{
			throw new NotImplementedException();
		}

		[IsUnit()]
		[ComponentInteraction("retirement-button")]
		public async Task CreateRetirementTicket()
		{
			throw new NotImplementedException();
		}

		[ComponentInteraction("donation-button")]
		public async Task CreateDonationTicket()
		{
			throw new NotImplementedException();
		}
	}
}