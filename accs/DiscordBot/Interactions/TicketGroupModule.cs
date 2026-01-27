using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Tickets;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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

        
        [SlashCommand("voice", "Создать приватный голосовой канал для участников тикета")]
        public async Task Voice()
        {
			try
			{
                int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
                Ticket? ticket = await _db.Tickets.FindAsync(ticketId);

                if (ticket == null)
                {
                    await RespondAsync($"Тикет с id {ticketId} не найден!", ephemeral: true);
                    await _logService.WriteAsync($"Ticket voice: Тикет {ticketId} не найден", LoggingLevel.Error);
                    return;
                }

                SocketGuild guild = _guildProvider.GetGuild();

                List<SocketGuildUser> participants = new();

                var author = guild.GetUser(ticket.AuthorDiscordId);
                if (author != null)
				{
                    participants.Add(author);
                }

                var adminPosts = ticket.GetAdmins(_db).ToList();
                await _db.Units.LoadAsync();

                foreach (var unit in _db.Units)
                {
                    if (unit.Posts.Intersect(adminPosts).Any())
                    {
                        var gu = guild.GetUser(unit.DiscordId);
                        if (gu != null)
                            participants.Add(gu);
                    }
                }

                participants = participants.Distinct().ToList();

                if (participants.Count == 0)
                {
                    await RespondAsync("Не удалось определить участников тикета.", ephemeral: true);
                    return;
                }

                if (!ulong.TryParse(DotNetEnv.Env.GetString("VOICE_CATEGORY_ID", "null"), out ulong voiceCategoryId))
                {
                    await RespondAsync("VOICE_CATEGORY_ID не найден в конфигурации.", ephemeral: true);
                    await _logService.WriteAsync("Ticket voice: Cannot parse VOICE_CATEGORY_ID", LoggingLevel.Error);
                    return;
                }

                var channel = await guild.CreateVoiceChannelAsync(
                    $"【🎧】Тикет {ticketId}",
                    props =>
                    {
                        props.CategoryId = voiceCategoryId;
                        props.Bitrate = 64000;
                        props.UserLimit = null;
                    }
                );

                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                    new OverwritePermissions(connect: PermValue.Deny));

                foreach (var user in participants)
                {
                    await channel.AddPermissionOverwriteAsync(user,
                        new OverwritePermissions(connect: PermValue.Allow));
                }

                await RespondAsync(
                    $"Приватный голосовой канал создан с id: {channel.Id}",
                    ephemeral: true
                );
            }
            catch (Exception ex)
			{
                await RespondAsync("Не удалось создать голосовой канал.", ephemeral: true); 
				await _logService.WriteAsync($"Ticket voice error: {ex.Message}", LoggingLevel.Error);
            }
        }
        

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
