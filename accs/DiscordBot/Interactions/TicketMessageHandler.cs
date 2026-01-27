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
			Console.WriteLine("HUH1");
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { await _logService.WriteAsync("Cannot parse ticket channel id!", LoggingLevel.Error); return; }
			Console.WriteLine("HUH2");
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { await _logService.WriteAsync("Ticket channel is null!", LoggingLevel.Error); return; }
			Console.WriteLine("HUH3");

			await _channel.DeleteMessagesAsync(await _channel.GetMessagesAsync().FlattenAsync());
			Console.WriteLine("HUH4");

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Тикеты")
				.WithDescription("Тикет — текстовое обращение, которое видят только Вы и нужная Вам служба.")
				.WithFooter(footer => footer.Text = "Чтобы сформировать тикет, нажмите на соответствующую кнопку ниже:")
				.WithAuthor(Context.Client.CurrentUser)
				.WithColor(Color.DarkGreen);
			Console.WriteLine("HUH5");

			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Вступить", "invite-button", ButtonStyle.Success, row: 0)
				.WithButton("Сотрудничать", "friend-button", ButtonStyle.Primary, row: 0)
				.WithButton("Инструктор", "lesson-button", ButtonStyle.Primary, row: 1)
				.WithButton("Техподдержка", "tech-button", ButtonStyle.Secondary, row: 1)
				.WithButton("Пожаловаться", "report-button", ButtonStyle.Danger, row: 2)
				.WithButton("Отставка", "retirement-button", ButtonStyle.Danger, row: 2)
				.WithButton("Спонсировать", "donation-button", ButtonStyle.Primary, row: 3);
			Console.WriteLine("HUH6");

			await _channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
			await RespondAsync("Сообщение для тикетов обновлено", ephemeral: true);
		}

		[IsUnit(false)]
		[ComponentInteraction("invite-button", ignoreGroupNames: true)]
		public async Task InviteButtonHandler()
		{
			InviteTicket ticket = new InviteTicket(Context.User.Id);
			await _db.InviteTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[IsUnit(false)]
		[ComponentInteraction("friend-button", ignoreGroupNames: true)]
		public async Task FriendButtonHandler()
		{
			FriendTicket ticket = new FriendTicket(Context.User.Id);
			await _db.FriendTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[ComponentInteraction("lesson-button", ignoreGroupNames: true)]
		public async Task LessonButtonHandler()
		{
			LessonTicket ticket = new LessonTicket(Context.User.Id);
			await _db.LessonTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[ComponentInteraction("tech-button", ignoreGroupNames: true)]
		public async Task TechButtonHandler()
		{
			TechTicket ticket = new TechTicket(Context.User.Id);
			await _db.TechTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[ComponentInteraction("report-button", ignoreGroupNames: true)]
		public async Task ReportButtonHandler()
		{
			ReportTicket ticket = new ReportTicket(Context.User.Id);
			await _db.ReportTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[IsUnit()]
		[ComponentInteraction("retirement-button", ignoreGroupNames: true)]
		public async Task RetirementButtonHandler()
		{
			RetirementTicket ticket = new RetirementTicket(Context.User.Id);
			await _db.RetirementTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}

		[ComponentInteraction("donation-button", ignoreGroupNames: true)]
		public async Task DonationButtonHandler()
		{
			DonationTicket ticket = new DonationTicket(Context.User.Id);
			await _db.DonationTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logService, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logService, _db);
		}
	}
}