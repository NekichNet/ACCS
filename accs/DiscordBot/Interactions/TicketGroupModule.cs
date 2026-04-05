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
		private readonly ILogger<TicketGroupModule> _log;
		private readonly AppDbContext _db;
		private readonly IGuildProviderService _guildProvider;

        public TicketGroupModule(ILogger<TicketGroupModule> log, AppDbContext db, IGuildProviderService guildProvider)
        { 
            _log = log;
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
				_log.LogError($"Тикет с id {ticketId} не найден!");
				return;
            }

			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (unit != null)
            {
				if (unit.Posts.Intersect(ticket.GetAdmins(_db)).Any() || unit.HasPermission(PermissionType.Administrator))
				{
					await ticket.AcceptAsync(_guildProvider, _db, unit.DiscordId);
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
				_log.LogError($"Тикет с id {ticketId} не найден!");
				return;
			}

			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (unit != null)
			{
				if (unit.Posts.Intersect(ticket.GetAdmins(_db)).Any() || unit.HasPermission(PermissionType.Administrator))
				{
					await ticket.RefuseAsync(_guildProvider, _db, unit.DiscordId);
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
				_log.LogError($"Тикет с id {ticketId} не найден!");
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
        public async Task VoiceCommand()
        {
			await DeferAsync(ephemeral: true);
			try
			{
                int ticketId = int.Parse(Context.Channel.Name.Split('-').Last());
                Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
				if (ticket == null)
                {
					await ModifyOriginalResponseAsync((props) => { props.Content = $"Тикет с id {ticketId} не найден!"; });
					_log.LogError($"Ticket voice: Тикет {ticketId} не найден");
                    return;
                }

				SocketGuild guild = _guildProvider.GetGuild();
                SocketGuildUser author = guild.GetUser(ticket.AuthorDiscordId);
				List<ulong> roleIds = ticket.GetAdmins(_db).Where(t => t.DiscordRoleId != null).Select(t => (ulong)t.DiscordRoleId).ToList();
				ulong voiceCategoryId = ulong.Parse(DotNetEnv.Env.GetString("VOICE_CATEGORY_ID", "null"));

				if (guild.VoiceChannels.Any(c => c.Name == $"【🎧】Тикет {ticketId}"))
				{
					await ModifyOriginalResponseAsync((props) => { props.Content = "Канал уже создан"; });
					return;
				}
				var channel = await guild.CreateVoiceChannelAsync(
                    $"【🎧】Тикет {ticketId}",
                    props =>
                    {
                        props.CategoryId = voiceCategoryId;
                        props.Bitrate = 64000;
                    }
                );
				await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                    new OverwritePermissions(connect: PermValue.Deny));
				await channel.AddPermissionOverwriteAsync(author, new OverwritePermissions(connect: PermValue.Allow));
				foreach (ulong roleId in roleIds)
                {
                    await channel.AddPermissionOverwriteAsync(await guild.GetRoleAsync(roleId), new OverwritePermissions(connect: PermValue.Allow));
                }
				await DeleteOriginalResponseAsync();
				await ReplyAsync($"Приватный голосовой канал создан: {(await channel.CreateInviteAsync()).Url}");
			}
            catch (Exception ex)
			{
				_log.LogError($"Ticket voice error: {ex.Message}");
				await ModifyOriginalResponseAsync((props) => { props.Content = "Произошла непредвиденная ошибка."; });
			}
        }
        

        [ComponentInteraction("invite-select-*", ignoreGroupNames: true)]
        public async Task InviteSelectHandler(int ticketId, int[] postIds)
        {
			int selectedId = postIds.First();
			Ticket? ticket = await _db.Tickets.FindAsync(ticketId);
			if (ticket is InviteTicket invite)
			{
				Unit? unit = await _db.Units.FindAsync(Context.User.Id);
				if (unit != null)
				{
					if (unit.Posts.Intersect(ticket.GetAdmins(_db)).Any())
					{
						await invite.AcceptanceHandler(selectedId, _guildProvider, _db, _log, unit.DiscordId);
					}
				}

				await RespondAsync("Выбрать взвод может только ответственная за тикет служба.", ephemeral: true);
			}
			else
			{
				_log.LogError($"Error: ticket {ticketId} is {ticket.GetType()}");
				await RespondAsync($"Ошибка: тикет с id {ticketId} не найден", ephemeral: true);
			}
        }

		[ComponentInteraction("retirement-select-*", ignoreGroupNames: true)]
		public async Task ReturnFromRetirementHandler(int ticketId, int[] postIds)
		{
			RetirementTicket? ticket = await _db.RetirementTickets.FindAsync(ticketId);
			if (ticket == null)
			{
				await RespondAsync($"Ошибка: тикет с Id {ticketId} не найден!", ephemeral: true);
				_log.LogError($"Тикет с Id {ticketId} не найден!");
				return;
			}

			var unit = await _db.Units.FindAsync(ticket.AuthorDiscordId);
			if (unit == null)
			{
				await RespondAsync($"Ошибка: боец c Id {ticket.AuthorDiscordId} не найден!", ephemeral: true);
				_log.LogError($"ReturnFromRetirenmentHandler: Боец c Id {ticket.AuthorDiscordId} не найден");
				return;
			}

			Unit? userUnit = await _db.Units.FindAsync(Context.User.Id);
			if (userUnit == null)
			{
				await RespondAsync("Ошибка: Вы не найдены в системе", ephemeral: true);
				return;
			}
			if (!userUnit.Posts.Intersect(ticket.GetAdmins(_db)).Any())
			{
				await RespondAsync("Выбор должностей доступен только ответственной за тикет службе", ephemeral: true);
				return;
			}

			// поиск активного статуса Retirement
			UnitStatus activeRetirement = unit.UnitStatuses
				.First(us =>
					us.Status.Type == StatusType.Retirement &&
					us.EndDate == null
				);

			activeRetirement.EndDate = DateTime.UtcNow;
			activeRetirement.RemoveRole(_guildProvider);

			unit.Posts.Clear();

			// Назначаем выбранные должности
			foreach (int id in postIds)
			{
				Post? post = await _db.Posts.FindAsync(id);
				if (post != null)
				{
					unit.Posts.Add(post);

					List<IRole> roles = new List<IRole>();
					if (post.DiscordRoleId != null)
						roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId));
					Subdivision? subdiv = post.Subdivision;
					while (subdiv != null)
					{
						if (subdiv.DiscordRoleId != null)
							roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)subdiv.DiscordRoleId));
						subdiv = subdiv.Head;
					}

					await _guildProvider.GetGuild().GetUser(ticket.AuthorDiscordId).AddRolesAsync(roles);
					await post.NotifyOnAssignAsync(Context.Guild, _db, unit);
				}
				else
					_log.LogError($"ReturnFromRetirenmentHandler: Post с id {id} не найден");
			}

			ticket.Status = TicketStatus.Accepted;
			_db.RetirementTickets.Update(ticket);

			await _db.SaveChangesAsync();
			await RespondAsync("Боец отправлен в отставку");
			await ticket.DeleteChannelAsync(_guildProvider);
		}
	}
}
