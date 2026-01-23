using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Tickets;
using accs.Repository;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [IsTicketChannel()]
    [Group("ticket", "Управление тикетами")]
    public class TicketGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
        private ITicketRepository _ticketRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;

        public TicketGroupModule(ITicketRepository ticketRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _ticketRepository = ticketRepository;
            _unitRepository = unitRepository;
            _logService = logService;
        }

        [SlashCommand("accept", "Принять")]
        public async Task Accept()
        {
            int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _ticketRepository.ReadAsync(ticketId);
			if (ticket == null)
            {
                await DeleteOriginalResponseAsync();
                await RespondAsync($"Тикет с id {ticketId} не найден!");
                await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
            }
			Unit? userUnit = await _unitRepository.ReadAsync(Context.User.Id);

			if (userUnit != null)
            {
				if (userUnit.Posts.Intersect(ticket.Admins).Any())
				{
                    await ticket.AcceptAsync();
                    return;
				} 
                else
                {
                    await _logService.WriteAsync("TicketGroupModule: unit " + userUnit.Nickname + " is not a ticket admin.", LoggingLevel.Debug);
                }
			}

            await RespondAsync("Принять тикет может только ответственная за него подчасть.", ephemeral: true);
			await DeleteOriginalResponseAsync();
		}

        [SlashCommand("refuse", "Отказать")]
        public async Task Refuse()
        {
			int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _ticketRepository.ReadAsync(ticketId);
			if (ticket == null)
			{
				await DeleteOriginalResponseAsync();
				await RespondAsync($"Тикет с id {ticketId} не найден!");
				await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
			}
			Unit? userUnit = await _unitRepository.ReadAsync(Context.User.Id);

			if (userUnit != null)
			{
				if (userUnit.Posts.Intersect(ticket.Admins).Any())
				{
					await ticket.RefuseAsync();
					return;
				}
				else
                {
                    await _logService.WriteAsync("TicketGroupModule: unit " + userUnit.Nickname + " is not a ticket admin.", LoggingLevel.Debug);
                }
			}
			await RespondAsync("Отказать и закрыть тикет может только ответственная за него подчасть.", ephemeral: true);
			await DeleteOriginalResponseAsync();
		}

        [SlashCommand("cancel", "Отменить")]
        public async Task Cancel()
        {
			int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
			Ticket? ticket = await _ticketRepository.ReadAsync(ticketId);
			if (ticket == null)
			{
				await DeleteOriginalResponseAsync();
				await RespondAsync($"Тикет с id {ticketId} не найден!");
				await _logService.WriteAsync($"Тикет с id {ticketId} не найден!", LoggingLevel.Error);
			}
			Unit? userUnit = await _unitRepository.ReadAsync(Context.User.Id);

			if (Context.User.Id == ticket.AuthorDiscordId)
            {
                await ticket.CancelAsync();
				return;
			}
			await RespondAsync("Отменить тикет может только автор тикета.", ephemeral: true);
			await DeleteOriginalResponseAsync();
		}

        /*
        [SlashCommand("voice", "Создать приватный голосовой канал для участников тикета")]
        public async Task Voice()
        {
            TODO
        }
        */

        [ComponentInteraction("invite-select-*")]
        public async Task InviteSelectHandler(int ticketId)
        {
            var component = (SocketMessageComponent)Context.Interaction;
            int selectedId = int.Parse(component.Data.Values.First());

            var ticket = await _ticketRepository.ReadAsync(ticketId);
            if (ticket is InviteTicket invite)
                await invite.AcceptanceHandler(selectedId);

            await RespondAsync("Рекрут успешно принят!");
        }
    }
}
