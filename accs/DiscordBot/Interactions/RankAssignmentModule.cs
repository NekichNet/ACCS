using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [HasPermission(PermissionType.ChangePosts)]
    public class RankAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly IRankRepository _rankRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogService _logService;
        private DiscordSocketClient _discordSocketClient;
        private SocketGuild? _guild;

        public RankAssignmentModule(IRankRepository rankRepository, IUnitRepository unitRepository, ILogService logService, DiscordSocketClient discordSocketClient)
        {
            _rankRepository = rankRepository;
            _unitRepository = unitRepository;
            _logService = logService;

            _discordSocketClient = discordSocketClient;

            string voiceChannelIdString = DotNetEnv.Env.GetString("VOICE_CHANNEL_ID", "null");

            string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
            ulong guildId;
            if (ulong.TryParse(guildIdString, out guildId)) { throw _logService.ExceptionAsync("Cannot parse guild id!", LoggingLevel.Error).Result; }

            _guild = _discordSocketClient.GetGuild(guildId);
        }

        [SlashCommand("set-ranks", "Назначить звания бойцу")]
        public async Task AssignRanksAsync(IUser targetedUser)
        {
            try
            {
                var actorUnit = await _unitRepository.ReadAsync(Context.User.Id);
                var targetUnit = await _unitRepository.ReadAsync(targetedUser.Id);

                if (targetUnit == null)
                {
                    await RespondAsync("Боец не найден.", ephemeral: true);
                    return;
                }

                var actorRanks = actorUnit.Rank;
                var allowedRanks = await _rankRepository.ReadAllAsync();
                
                /*var allowedRanks = actorRanks
                        .SelectMany(p => p.GetAllSubordinatesRecursive())
                        .DistinctBy(p => p.Id)
                        .ToList();*/

                if (!allowedRanks.Any())
                {
                    await RespondAsync("Нет доступных должностей для назначения.", ephemeral: true);
                    return;
                }

                var menu = new SelectMenuBuilder()
                    .WithCustomId($"rank-menu-{targetedUser.Id}")
                    .WithPlaceholder("Выберите звания")
                    .WithMinValues(0)
                    .WithMaxValues(allowedRanks.Count);

                foreach (var post in allowedRanks)
                    menu.AddOption(post.GetFullName(), post.Id.ToString());

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menu);

                await RespondAsync(
                    $"Назначение званий для {targetUnit.Nickname}",
                    components: builder.Build(),
                    ephemeral: true);

            }
            catch (Exception ex)
            {
                _logService.WriteAsync(ex.Message, LoggingLevel.Error);
            }
        }
    }
}
