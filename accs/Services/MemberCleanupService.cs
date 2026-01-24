using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord.WebSocket;

namespace accs.Services
{
    public class MemberCleanupService : IMemberCleanupService
    {
        private readonly DiscordSocketClient _client;
        private readonly IMemberPresenceRepository _presenceRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogService _logService;

        private readonly int _daysBeforeKick = DotNetEnv.Env.GetInt("MEMBER_KICK_AFTER_DAYS", 7);


        public MemberCleanupService(DiscordSocketClient client, IMemberPresenceRepository presenceRepository, ITicketRepository ticketRepository, ILogService logService, int daysBeforeKick)
        {
            _client = client;
            _presenceRepository = presenceRepository;
            _ticketRepository = ticketRepository;
            _logService = logService;
            _daysBeforeKick = daysBeforeKick;
        }

        public async Task CleanupAsync(SocketGuild guild)
        {
            var users = guild.Users;
            var presences = await _presenceRepository.ReadAllAsync();

            foreach (var user in users)
            {
                if (user.IsBot)
                    continue;

                // если пользователя нет в базе — добавляем
                var presence = presences.FirstOrDefault(p => p.DiscordId == user.Id);
                if (presence == null)
                {
                    presence = new MemberPresence
                    {
                        DiscordId = user.Id,
                        JoinedAt = DateTime.UtcNow
                    };

                    await _presenceRepository.CreateAsync(presence);
                }

                if (user == null)
                {
                    await _presenceRepository.DeleteAsync(presence);
                    continue;
                }

                // если у пользователя есть роли — удаление из базы
                if (user.Roles.Any(r => r.Id != guild.EveryoneRole.Id))
                {
                    await _presenceRepository.DeleteAsync(presence);
                    continue;
                }

                // проверка открытых тикетов
                var tickets = await _ticketRepository.ReadAllAsync();
                bool hasOpenTicket = tickets.Any(t =>
                    t.AuthorDiscordId == user.Id &&
                    t.Status != TicketStatus.Canceled
                );

                if (hasOpenTicket)
                    continue;

                // проверка срок
                if ((DateTime.UtcNow - presence.JoinedAt).TotalDays >= _daysBeforeKick)
                {
                    await user.KickAsync("Не получил ни одной роли за N дней");
                    await _logService.WriteAsync($"Кикнут пользователь {user.Id} за отсутствие ролей.");

                    await _presenceRepository.DeleteAsync(presence);
                }
            }
        }
    }
}
