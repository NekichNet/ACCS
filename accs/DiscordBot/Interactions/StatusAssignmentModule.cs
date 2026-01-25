using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.DiscordBot.Interactions
{
    public class StatusAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;

        public StatusAssignmentModule(AppDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        [HasPermission(PermissionType.GiveReprimandGratitude)]
        [SlashCommand("give", "Выдать благодарность или выговор")] public async Task GiveCommandAsync(IUser user, 
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

                await _db.UnitStatuses.LoadAsync();
                Unit? unit = await _db.Units.FindAsync(user.Id);
                Status? status = await _db.Statuses.FindAsync(givenType);

                if (unit != null && status != null)
                {
                    var unitStatus = new UnitStatus() { Unit = unit, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(amountOfDays), Status = status };
                    await _db.UnitStatuses.AddAsync(unitStatus);
                }
                else
                {
                    throw new Exception("Пользователь для выдачи статуса или сам статус не найден!");
                }

            }
            catch (Exception e) 
            {
                await RespondAsync("При присвоении статуса произошла необработанная ошибка!", ephemeral: true);
                await _logService.WriteAsync(e.Message, LoggingLevel.Error);
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }

        [HasPermission(PermissionType.VacationAccess)]
        [SlashCommand("vacation", "Выход в отпуск")]
        public async Task VacationCommandAsync([MinValue(1), MaxValue(7)] int days = 7)
        {
            try
            {
                await _db.UnitStatuses.LoadAsync();

                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не найдены в системе.", ephemeral: true);
                    await _logService.WriteAsync($"VacationCommandAsync: Боец {Context.User.Username} с Id {Context.User.Id} не найден в базе", LoggingLevel.Error);
                    return;
                }

                Status? vacationStatus = await _db.Statuses.FindAsync(StatusType.Vacation);
                if (vacationStatus == null)
                {
                    await RespondAsync("Статус 'Отпуск' не найден в базе.", ephemeral: true);
					await _logService.WriteAsync($"VacationCommandAsync: Статус 'Отпуск' не найден в базе.", LoggingLevel.Error);
					return;
                }

                DateTime endDate = DateTime.Now.AddDays(days);
				var unitStatus = new UnitStatus()
                {
                    Unit = unit,
                    Status = vacationStatus,
                    StartDate = DateTime.Now,
                    EndDate = endDate
                };

                await _db.UnitStatuses.AddAsync(unitStatus);

                await RespondAsync($"Вы успешно вышли в отпуск на {days} дней до {endDate:d}.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await RespondAsync("Не удалось оформить отпуск.", ephemeral: true); 
                await _logService.WriteAsync(ex.Message, LoggingLevel.Error);
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }
    }
}
