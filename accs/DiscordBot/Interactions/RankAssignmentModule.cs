using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [HasPermission(PermissionType.ChangeRanks)]
    public class RankAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly IRankRepository _rankRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogService _logService;

        public RankAssignmentModule(IRankRepository rankRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _rankRepository = rankRepository;
            _unitRepository = unitRepository;
            _logService = logService;


            string voiceChannelIdString = DotNetEnv.Env.GetString("VOICE_CHANNEL_ID", "null");

            string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
            ulong guildId;
            if (ulong.TryParse(guildIdString, out guildId)) { throw _logService.ExceptionAsync("Cannot parse guild id!", LoggingLevel.Error).Result; }

        }

        [SlashCommand("set-rank", "Присвоить звание бойцу")]
        public async Task SetRankAsync(IUser targetedUser)
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


                /// На чёрный день
                /*
                var allowedRanks = new List<Rank>(); 
                Rank CurrentTestRank = actorUnit.Rank;
                while (CurrentTestRank.Previous != null)
                {
                    CurrentTestRank = CurrentTestRank.Previous;
                    allowedRanks.Add(CurrentTestRank);
                }
                */

                var allowedRanks = await _rankRepository.ReadAllAsync();
                


                if (!allowedRanks.Any())
                {
                    await RespondAsync("Нет доступных должностей для назначения.", ephemeral: true);
                    return;
                }

                var menu = new SelectMenuBuilder()
                    .WithCustomId($"rank-menu-{targetedUser.Id}")
                    .WithPlaceholder("Выберите звания")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var rank in allowedRanks)
                    menu.AddOption(rank.Name, rank.Id.ToString());

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


        [ComponentInteraction("rank-menu-*")]
        public async Task RankMenuHandler(ulong targetId)
        {
            try
            {
                var component = (SocketMessageComponent)Context.Interaction;
                var targetUnit = await _unitRepository.ReadAsync(targetId);
                var promptedRank = component.Data.Values.ElementAt(0);
                var rank = await _rankRepository.ReadAsync(Int32.Parse(promptedRank));

                if (rank == null)
                {
                    await RespondAsync($"Звание {promptedRank} не найдено.", ephemeral: true);
                    await _logService.WriteAsync($"Должность с ID {promptedRank} не найдена.", LoggingLevel.Error);
                    return;
                }

                // Присвоение звания
                targetUnit.Rank = rank;

                await _unitRepository.UpdateAsync(targetUnit);

                await RespondAsync("Должности обновлены.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка в RankMenuHandler: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при обновлении должностей.", ephemeral: true);
            }
        }
    }
}
