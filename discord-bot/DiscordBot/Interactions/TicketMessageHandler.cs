using accs.Database;
using accs.Controllers.DiscordBot.Preconditions;
using accs.Models.Database;
using accs.Models.Database.Tickets;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace discord_bot.DiscordBot.Interactions
{
	public class TicketMessageHandler : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly AppDbContext _db;
		private readonly ILogger<TicketMessageHandler> _log;
		private readonly ILogger<Ticket> _logTicket;
		private readonly IGuildProviderService _guildProvider;

		public TicketMessageHandler(DiscordSocketClient client, AppDbContext db, ILogger<TicketMessageHandler> log, ILogger<Ticket> logTicket, IGuildProviderService guildProvider)
		{
			_client = client;
			_db = db;
			_log = log;
			_logTicket = logTicket;
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
			await DeferAsync(ephemeral: true);

			/*
			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (!ulong.TryParse(channelIdString, out channelId)) { _log.LogError("Cannot parse ticket channel id!"); return; }
			SocketTextChannel _channel = (SocketTextChannel)_client.GetChannel(channelId);
			if (_channel == null) { _log.LogError("Ticket channel is null!"); return; }
			*/

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Тикеты")
				.WithDescription("Тикет — текстовое обращение, которое видят только Вы и нужная Вам служба.")
				.WithFooter("Чтобы сформировать тикет, нажмите на соответствующую кнопку ниже:")

				.AddField("*Вступить*", "Подать заявку на вступление в клан.")
				.AddField("*Сотрудничать*", "Если хотите сотрудничать, получить роль «Друг клана» и доступ к голосовым каналам.")
				.AddField("*Спонсировать*", "При намерении финансово поддержать наш клан.")
				.AddField("*Награждение*", "Подать тикет на представление к заслуженной награде.")
				.AddField("*Инструктор*", "Попросить инструкторский корпус об уроке, либо задать вопрос по игре.")

				.AddField("*Пожаловаться*", "Подать жалобу на находящегося на этом сервере пользователя(ей).")
				.AddField("*Отставка*", "Уйти в отставку или выйти из неё.")
				.AddField("*Получить VIP*", "Подать заявку на получение VIP статуса на игровых серверах, которые мы посещаем.")
				.AddField("Предложка", "Если хотите предложить нововедения для улучшения жизни в клане, то вам сюда.")
				.AddField("*Техподдержка*", "Если обнаружили техническую неисправность или испытываете затруднение" +
					" в пользовании сайтом или ботом — нажимаем.")

				.WithColor(Color.DarkGreen);

			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Вступить", "invite-button", ButtonStyle.Success, row: 0)
				.WithButton("Сотрудничать", "friend-button", ButtonStyle.Primary, row: 0)
				.WithButton("Спонсировать", "donation-button", ButtonStyle.Primary, row: 0)
				.WithButton("Награждение", "reward-button", ButtonStyle.Primary, row: 0)
				.WithButton("Инструктор", "lesson-button", ButtonStyle.Primary, row: 0)

				.WithButton("Пожаловаться", "report-button", ButtonStyle.Danger, row: 1)
				.WithButton("Отставка", "retirement-button", ButtonStyle.Danger, row: 1)
				.WithButton("Получить VIP", "vip-button", ButtonStyle.Secondary, row: 1)
				.WithButton("Предложка", "suggestion-button", ButtonStyle.Secondary, row: 1)
				.WithButton("Техподдержка", "tech-button", ButtonStyle.Secondary, row: 1);

			await Context.Channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
			await DeleteOriginalResponseAsync();
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
			await RespondAsync("Тикет на вступление создан. Пожалуйста, выполните дальнейшие инструкции", ephemeral: true);
		}

		[ComponentInteraction("friend-button", ignoreGroupNames: true)]
		public async Task FriendButtonHandler()
		{
			Unit? unit = await _db.Units.FindAsync(Context.User.Id);

			if (unit != null)
			{
				if (unit.Posts.Any())
				{
					await RespondAsync("Вы уже состоите в клане и не находитесь в отставке.", ephemeral: true);
					return;
				}
			}

			if (_db.FriendTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на сотрудничество", ephemeral: true);
				return;
			}

			FriendTicket ticket = new FriendTicket(Context.User.Id);
			await _db.FriendTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
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
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
			await RespondAsync("Спасибо, что решили финансово поддержать наш клан. Свяжитесь с руководством в канале тикета.", ephemeral: true);
		}

		[ComponentInteraction("vip-button", ignoreGroupNames: true)]
		public async Task VipButtonHandler()
		{
			if (_db.VipTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на получение VIP", ephemeral: true);
				return;
			}

			VipTicket ticket = new VipTicket(Context.User.Id);
			await _db.VipTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
			await RespondAsync("Заявка на получение VIP статуса создана. Перейдите в чат тикета.", ephemeral: true);
		}

		[ComponentInteraction("reward-button", ignoreGroupNames: true)]
		public async Task RewardButtonHandler()
		{
			/*
			if (_db.RewardTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет на представление к награде", ephemeral: true);
				return;
			}
			*/

			RewardTicket ticket = new RewardTicket(Context.User.Id);
			await _db.RewardTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
			await RespondAsync("Тикет на представление к награде создан. Перейдите в чат тикета.", ephemeral: true);
		}

		[ComponentInteraction("suggestion-button", ignoreGroupNames: true)]
		public async Task SuggestionButtonHandler()
		{
			/*
			if (_db.SuggestionTickets.Any(t => t.AuthorDiscordId == Context.User.Id && t.Status == TicketStatus.Opened))
			{
				await RespondAsync("У Вас уже есть открытый тикет жалобы", ephemeral: true);
				return;
			}
			*/

			SuggestionTicket ticket = new SuggestionTicket(Context.User.Id);
			await _db.SuggestionTickets.AddAsync(ticket);
			await _db.SaveChangesAsync();
			await ticket.CreateChannelAsync(_guildProvider, _logTicket, _db);
			await ticket.SendWelcomeMessageAsync(_guildProvider, _logTicket, _db);
			await RespondAsync("Предложение создано. Пожалуйста, опишите его в канале тикета.", ephemeral: true);
		}
	}
}