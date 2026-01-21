using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Protobuf;

namespace accs.DiscordBot.Interactions
{
    public class StatusAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogService _logService;
        private readonly IStatusRepository _statusRepository;
        private readonly IUnitStatusRepository _unitStatusRepository;

        public StatusAssignmentModule(IPostRepository postRepository, IUnitRepository unitRepository, ILogService logService, IUnitStatusRepository unitStatusRepository, IStatusRepository statusRepository, DiscordSocketClient discordSocketClient)
        {
            _unitRepository = unitRepository;
            _logService = logService;
            _statusRepository = statusRepository;
            _unitStatusRepository = unitStatusRepository;


        }
        [HasPermission(PermissionType.GiveReprimandGratitude)]
        [SlashCommand("give", "Управление санкциями")] public async Task GiveCommandAsync(IUser user, 
            [Choice("gratitude", "gratitude"), Choice("reprimand", "reprimand"), Choice("severe-reprimand", "severeReprimand")] string statusType, int amountOfDays = 7)
        {
            try
            {

                StatusType givenType;
                if (statusType == "gratitude")
                {
                    givenType = StatusType.Gratitude;
                }
                else if (statusType == "reprimand")
                {

                    givenType = StatusType.Reprimand;
                }
                else if (statusType == "severeReprimand")
                {

                    givenType = StatusType.SevereReprimand;
                }
                else
                {
                    throw new Exception("Не удалось спарсить статус!");
                }

                Unit? unit = await _unitRepository.ReadAsync(user.Id);
                Status? fetchedStatus = await _statusRepository.ReadAsync(givenType);

                if (unit != null && fetchedStatus != null)
                {
                    var status = new UnitStatus() { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(amountOfDays), Status = fetchedStatus };
                    await _unitStatusRepository.CreateAsync(status);
                    unit.UnitStatuses.Add(status);
                    await _unitRepository.UpdateAsync(unit);
                }
                else
                {
                    throw new Exception("Пользователь для выдачи статуса или сам статус не найден!");
                }

            }
            catch (Exception e) 
            {
                await RespondAsync("Не удалось присвоить статус.", ephemeral: true);
                await _logService.WriteAsync(e.Message);
                return;
            }
        }
    }
}
