using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models.Enums;
using accs.Models.Tickets;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
	[InChannels("TICKET_CHANNEL_ID")]
	public class TicketMessageHandler : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly AppDbContext _db;
		private readonly ILogService _logService;
		private readonly IGuildProviderService _guildProvider;

		public TicketMessageHandler(DiscordSocketClient client, AppDbContext db, ILogService logService, IGuildProviderService guildProvider)
		{
			_client = client;
			_db = db;
			_logService = logService;
			_guildProvider = guildProvider;
		}

		/*
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);

			_client.Connected += InitTicketMessage;
        }
		*/

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
				.WithButton("Вступить", "invite-button", ButtonStyle.Success, row: 0)
				.WithButton("Сотрудничать", "friend-button", ButtonStyle.Primary, row: 0)
				.WithButton("Инструктор", "lesson-button", ButtonStyle.Primary, row: 1)
				.WithButton("Техподдержка", "tech-button", ButtonStyle.Secondary, row: 1)
				.WithButton("Пожаловаться", "report-button", ButtonStyle.Danger, row: 2)
				.WithButton("Отставка", "retirement-button", ButtonStyle.Danger, row: 2)
				.WithButton("Спонсировать", "donation-button", ButtonStyle.Primary, row: 3);

			await _channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
			await RespondAsync("Сообщение для тикетов обновлено", ephemeral: true);
		}

		[IsUnit(false)]
		[ComponentInteraction("invite-button", ignoreGroupNames: true)]
		public async Task InviteButtonHandler()
		{
			if (_db.InviteTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на вступление", ephemeral: true);
				return;
			}

			InviteTicket ticket = new InviteTicket(Context.User.Id);
			await _db.InviteTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Тикет на вступление создан. Пожалуйста, выполните дальнейшие инструкции", ephemeral: true);
		}

		[IsUnit(false)]
		[ComponentInteraction("friend-button", ignoreGroupNames: true)]
		public async Task FriendButtonHandler()
		{
			if (_db.FriendTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на сотрудничество", ephemeral: true);
				return;
			}

			FriendTicket ticket = new FriendTicket(Context.User.Id);
			await _db.FriendTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Тикет на сотрудничество создан. Пожалуйста, раскройте свой запрос письменно в канале тикета.", ephemeral: true);
		}

		[ComponentInteraction("lesson-button", ignoreGroupNames: true)]
		public async Task LessonButtonHandler()
		{
			if (_db.LessonTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый запрос инструкторам", ephemeral: true);
				return;
			}

			LessonTicket ticket = new LessonTicket(Context.User.Id);
			await _db.LessonTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Тикет инструкторам создан. Пожалуйста, раскройте свой запрос письменно в канале тикета.", ephemeral: true);
		}

		[ComponentInteraction("tech-button", ignoreGroupNames: true)]
		public async Task TechButtonHandler()
		{
			if (_db.TechTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет техподдержке", ephemeral: true);
				return;
			}

			TechTicket ticket = new TechTicket(Context.User.Id);
			await _db.TechTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Тикет техподдержке создан. Пожалуйста, опишите проблему или предложение в канале тикета.", ephemeral: true);
		}

		[ComponentInteraction("report-button", ignoreGroupNames: true)]
		public async Task ReportButtonHandler()
		{
			if (_db.ReportTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет жалобы", ephemeral: true);
				return;
			}

			ReportTicket ticket = new ReportTicket(Context.User.Id);
			await _db.ReportTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Жалоба создана. Пожалуйста, опишите её в канале тикета.", ephemeral: true);
		}

		[IsUnit()]
		[ComponentInteraction("retirement-button", ignoreGroupNames: true)]
		public async Task RetirementButtonHandler()
		{
			if (_db.RetirementTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый запрос на отставку", ephemeral: true);
				return;
			}

			RetirementTicket ticket = new RetirementTicket(Context.User.Id);
			await _db.RetirementTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Запрос на отставку создан. Пожалуйста, опишите причину в канале тикета.", ephemeral: true);
		}

		[ComponentInteraction("donation-button", ignoreGroupNames: true)]
		public async Task DonationButtonHandler()
		{
			if (_db.DonationTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на пожертвование клану", ephemeral: true);
				return;
			}

			DonationTicket ticket = new DonationTicket(Context.User.Id);
			await _db.DonationTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
			await RespondAsync("Спасибо, что решили финансово поддержать наш клан. Свяжитесь с руководством в канале тикета.", ephemeral: true);
		}
	}
}