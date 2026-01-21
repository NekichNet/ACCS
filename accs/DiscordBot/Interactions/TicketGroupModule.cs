using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    [IsTicketChannel()]
    [Group("ticket", "Управление тикетами")]
    public class TicketGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
        private ITicketRepository _ticketRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;
        private Ticket? _ticket;
        private Unit? _userUnit; // на тот случай, если выполняющий команду состоит в клане.

        public TicketGroupModule(ITicketRepository ticketRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _ticketRepository = ticketRepository;
            _unitRepository = unitRepository;
            _logService = logService;
            _ticket = _ticketRepository.ReadAsync(int.Parse(Context.Channel.Name.Split('-').Last())).Result;
            if (_ticket == null) { throw _logService.ExceptionAsync("TicketGroupModule: Ticket not found by id!").Result; }
            _userUnit = _unitRepository.ReadAsync(Context.User.Id).Result;
            _logService.WriteAsync("TicketGroupModule: UserUnit: " + _userUnit?.Nickname, LoggingLevel.Debug);
        }

        [SlashCommand("accept", "Принять")]
        public async Task Accept()
        {
            if (_userUnit != null)
            {
				if (_userUnit.Posts.Intersect(_ticket.Admins).Any())
				{
                    await _ticket.Accept();
                    return;
				} 
                else
                {
                    await _logService.WriteAsync("TicketGroupModule: unit " + _userUnit.Nickname + " is not a ticket admin.", LoggingLevel.Debug);
                }
			}

            await RespondAsync("Принять тикет может только ответственная за него подчасть.", ephemeral: true);
			await DeleteOriginalResponseAsync();
		}

        [SlashCommand("refuse", "Отказать")]
        public async Task Refuse()
        {
			if (_userUnit != null)
			{
				if (_userUnit.Posts.Intersect(_ticket.Admins).Any())
				{
					await _ticket.Refuse();
					return;
				}
				else
                {
                    await _logService.WriteAsync("TicketGroupModule: unit " + _userUnit.Nickname + " is not a ticket admin.", LoggingLevel.Debug);
                }
			}
			await RespondAsync("Отказать и закрыть тикет может только ответственная за него подчасть.", ephemeral: true);
			await DeleteOriginalResponseAsync();
		}

        [SlashCommand("cancel", "Отменить")]
        public async Task Cancel()
        {
            if (Context.User.Id == _ticket.AuthorDiscordId)
            {
                await _ticket.Cancel();
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
    }
}
