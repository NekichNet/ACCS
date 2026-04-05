using accs.Database;
using accs.Logging.EventIds;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Services
{
    public class UsersCleanupService : IUsersCleanUpService
    {
        private readonly SocketGuild _guild;
        private readonly ILogger<UsersCleanupService> _log;
        private readonly AppDbContext _db;

        public int DaysTimer { get; set; }

        public UsersCleanupService(IGuildProviderService guildProvider, AppDbContext db, ILogger<UsersCleanupService> log)
        {
            _guild = guildProvider.GetGuild();
            _log = log;
            _db = db;
            DaysTimer = DotNetEnv.Env.GetInt("CLEANUP_TIMER", 5);
		}

        public async Task CleanupAsync()
        {
            await foreach (SocketGuildUser user in _guild.GetUsersAsync())
            {
                if (user.IsBot)
                {
                    continue;   
                }

                DateTimeOffset? days = user.JoinedAt;
                if (days == null)
                {
                    _log.LogWarning(EventIds.NoData, $"Cannot read {user.DisplayName}'s user.JoinedAt, continuing.");
                    continue;
                }

				if (!user.Roles.Any(r => r.Id != _guild.EveryoneRole.Id)
                    && ((DateTimeOffset)days).Day > DaysTimer
                    && !_db.Tickets.Any(t => t.AuthorDiscordId == user.Id && t.Status == TicketStatus.Opened))
                {
                    _log.LogInformation($"Кик пользователя {user.DisplayName} за бездействие.");
                    await user.KickAsync($"Вы находитесь на сервере РХБЗ дольше {DaysTimer} дней без роли");
                }
            }
        }
    }
}
