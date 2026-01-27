using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Tickets;
using accs.Services.Interfaces;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.DiscordBot.Interactions
{
    [IsTicketChannel()]
    [Group("ticket", "Управление тикетами")]
    public class TicketGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly ILogService _logService;
		private readonly AppDbContext _db;
		private readonly IGuildProviderService _guildProvider;

        public TicketGroupModule(ILogService logService, AppDbContext db, IGuildProviderService guildProvider)
        { 
            _logService = logService;
			_db = db;
			_guildProvider = guildProvider;
        }

        [SlashCommand("accept", "Принять")]
        public async Task AcceptCommand()
        {
            int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
			if (ticket == null)
            {
                await RespondAsync($"Тикет с id {ticketId} не найден!");
                await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
				return;
            }

			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (unit != null)
            {
				if (unit.Posts.Intersect(ticket.GetAdmins(_db)).Any())
				{
					await ticket.AcceptAsync(_guildProvider, _db);
					return;
				}
			}

			await RespondAsync("Принять тикет может только ответственная за него подчасть.", ephemeral: true);
		}

        [SlashCommand("refuse", "Отказать")]
        public async Task RefuseCommand()
        {
			int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				await RespondAsync($"Тикет с id {ticketId} не найден!");
				await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
				return;
			}

			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (unit != null)
			{
				if (unit.Posts.Intersect(ticket.GetAdmins(_db)).Any())
				{
					await ticket.RefuseAsync(_guildProvider, _db);
					return;
				}
			}

			await RespondAsync("Отказать и закрыть тикет может только ответственная за него подчасть.", ephemeral: true);
		}

        [SlashCommand("cancel", "Отменить")]
        public async Task CancelCommand()
        {
			int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				await RespondAsync($"Тикет с id {ticketId} не найден!");
				await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
				return;
			}

			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (Context.User.Id == ticket.AuthorDiscordId)
            {
                await ticket.CancelAsync(_guildProvider, _db);
				return;
			}

			await RespondAsync("Отменить тикет может только автор тикета.", ephemeral: true);
		}

        /*
        [SlashCommand("voice", "Создать приватный голосовой канал для участников тикета")]
        public async Task Voice()
        {
            TODO
        }
        */

        [ComponentInteraction("invite-select-*")]
        public async Task InviteSelectHandler(int ticketId, int[] postIds)
        {
            int selectedId = postIds.First();
            Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket is InviteTicket invite)
			{
				await invite.AcceptanceHandler(selectedId, _guildProvider, _db, _logService);
			}
			else
			{
				await _logService.WriteAsync($"Error: ticket {ticketId} is {ticket.GetType()}");
				await RespondAsync($"Ошибка: тикет с id {ticketId} не найден", ephemeral: true);
			}
        }

		[ComponentInteraction("retirement-select-*")]
		public async Task ReturnFromRetirementHandler(int ticketId, int[] postIds)
		{
			await _db.UnitStatuses.LoadAsync();
			await _db.Posts.LoadAsync();

			Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				await RespondAsync($"Ошибка: тикет с Id {ticketId} не найден!", ephemeral: true);
				await _logService.WriteAsync($"Тикет с Id {ticketId} не найден!", LoggingLevel.Error);
				return;
			}

			var unit = await _db.Units.FindAsync(ticket.AuthorDiscordId);
			if (unit == null)
			{
				await RespondAsync($"Ошибка: боец c Id {ticket.AuthorDiscordId} не найден!", ephemeral: true);
				await _logService.WriteAsync($"ReturnFromRetirenmentHandler: Боец c Id {ticket.AuthorDiscordId} не найден", LoggingLevel.Error);
				return;
			}

			// поиск активного статуса Retirement
			UnitStatus? activeRetirement = unit.UnitStatuses
				.FirstOrDefault(us =>
					us.Status.Type == StatusType.Retirement &&
					us.EndDate > DateTime.UtcNow
				);

			if (activeRetirement != null)
			{
				activeRetirement.EndDate = DateTime.UtcNow;
			}

			unit.Posts.Clear();

			// Назначаем выбранные должности
			foreach (int id in postIds)
			{
				Post? post = await _db.Posts.FindAsync(id);
				if (post != null)
					unit.Posts.Add(post);
				else
					await _logService.WriteAsync($"ReturnFromRetirenmentHandler: Post с id {id} не найден", LoggingLevel.Error);
			}

			ticket.Status = TicketStatus.Accepted;

			await _db.SaveChangesAsync();
			await ticket.DeleteChannelAsync(_guildProvider);
		}
	}
}
