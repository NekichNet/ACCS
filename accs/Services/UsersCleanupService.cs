using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Services
{
    public class UsersCleanupService : IUsersCleanUpService
    {
        private readonly SocketGuild _guild;
        private readonly ILogService _logService;
        private readonly AppDbContext _db;

        public int DaysTimer { get; set; }

        public UsersCleanupService(IGuildProviderService guildProvider, AppDbContext db, ILogService logService)
        {
            _guild = guildProvider.GetGuild();
            _logService = logService;
            _db = db;
            DaysTimer = DotNetEnv.Env.GetInt("CLEANUP_TIMER", 5);
		}

        public async Task CleanupAsync()
        {
            foreach (SocketGuildUser user in _guild.Users)
            {
                if (user.IsBot)
                    continue;

                DateTimeOffset? days = user.JoinedAt;
                if (days == null)
                {
                    await _logService.WriteAsync("Cannot read user.JoinedAt, stopping cleanup.", LoggingLevel.Error);
                    return;
                }

				if (!user.Roles.Any(r => r.Id != _guild.EveryoneRole.Id)
                    && ((DateTimeOffset)days).Day > DaysTimer
                    && !_db.Tickets.Any(t => t.AuthorDiscordId == user.Id && t.Status == TicketStatus.Opened))
                {
                    await user.KickAsync($"Вы находитесь на сервере РХБЗ дольше {DaysTimer} дней без роли");
                }
            }
        }
    }
}
