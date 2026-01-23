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


        [HasPermission(PermissionType.VacationAccess)]
        [SlashCommand("vacation", "Выход в отпуск")]
        public async Task VacationCommandAsync(int days = 7)
        {
            try
            {
                Unit? unit = await _unitRepository.ReadAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не зарегистрированы как боец.", ephemeral: true);
                    return;
                }

                Status? vacationStatus = await _statusRepository.ReadAsync(StatusType.Vacation);
                if (vacationStatus == null)
                {
                    await RespondAsync("Статус 'Отпуск' не найден в базе.", ephemeral: true);
                    return;
                }

                var unitStatus = new UnitStatus()
                {
                    Unit = unit,
                    Status = vacationStatus,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(days)
                };

                await _unitStatusRepository.CreateAsync(unitStatus);

                unit.UnitStatuses.Add(unitStatus);
                await _unitRepository.UpdateAsync(unit);

                await RespondAsync(
                    $"Вы успешно вышли в отпуск на {days} дней.",
                    ephemeral: true
                );
            }
            catch (Exception ex)
            {
                await RespondAsync("Не удалось оформить отпуск.", ephemeral: true); 
                await _logService.WriteAsync(ex.Message); 
                return;
            }
        }
    }
}
