using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Tickets;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Channels;

namespace accs.DiscordBot.Interactions
{
	[InChannels("TICKET_CHANNEL_ID")]
	public class TicketMessageHandler : InteractionModuleBase<SocketInteractionContext>
	{
		private DiscordSocketClient _client;
		private ILogService _logService;
		private IPostRepository _postRepository;
		private IUnitRepository _unitRepository;
		private IRankRepository _rankRepository;
		private IStatusRepository _statusRepository;
		private IUnitStatusRepository _unitStatusRepository;

		public TicketMessageHandler(DiscordSocketClient client, ILogService logService,
			IPostRepository postRepository, IUnitRepository unitRepository,
			IRankRepository rankRepository, IStatusRepository statusRepository,
			IUnitStatusRepository unitStatusRepository)
		{
			_client = client;
			_logService = logService;
			_postRepository = postRepository;
			_unitRepository = unitRepository;
			_rankRepository = rankRepository;
			_statusRepository = statusRepository;
			_unitStatusRepository = unitStatusRepository;
		}

		[HasPermission(PermissionType.Administrator)]
		[SlashCommand("init-ticket-message", "Инициализация сообщения для тикетов.")]
		public async Task InitTicketMessage()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

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
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new InviteTicket(_channel.Guild, Context.User.Id, _channel.Id, _postRepository, _unitRepository, _rankRepository, _logService);
			await ticket.SendWelcomeMessageAsync();
		}

		[IsUnit(false)]
		[ComponentInteraction("friend-button")]
		public async Task CreateFriendTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new FriendTicket(_channel.Guild, Context.User.Id, _channel.Id);
			await ticket.SendWelcomeMessageAsync();
		}

		[ComponentInteraction("lesson-button")]
		public async Task CreateLessonTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new LessonTicket(_channel.Guild, Context.User.Id, _channel.Id);
			await ticket.SendWelcomeMessageAsync();
		}

		[ComponentInteraction("tech-button")]
		public async Task CreateTechTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new TechTicket(_channel.Guild, Context.User.Id, _channel.Id);
			await ticket.SendWelcomeMessageAsync();
		}

		[ComponentInteraction("report-button")]
		public async Task CreateReportTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new ReportTicket(_channel.Guild, Context.User.Id, _channel.Id);
			await ticket.SendWelcomeMessageAsync();
		}

		[IsUnit()]
		[ComponentInteraction("retirement-button")]
		public async Task CreateRetirementTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new RetirementTicket(_channel.Guild, Context.User.Id, _channel.Id, _unitRepository, _postRepository, _statusRepository, _unitStatusRepository);
			await ticket.SendWelcomeMessageAsync();
		}

		[ComponentInteraction("donation-button")]
		public async Task CreateDonationTicket()
		{
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }

			Ticket ticket = new DonationTicket(_channel.Guild, Context.User.Id, _channel.Id);
			await ticket.SendWelcomeMessageAsync();
		}
	}
}