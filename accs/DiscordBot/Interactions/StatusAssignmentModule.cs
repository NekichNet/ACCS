using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    public class StatusAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;
        private readonly IGuildProviderService _guildProvider;

        public StatusAssignmentModule(AppDbContext db, ILogService logService, IGuildProviderService guildProvider)
        {
            _db = db;
            _logService = logService;
            _guildProvider = guildProvider;
        }

        [HasPermission(PermissionType.GiveReprimandGratitude)]
        [SlashCommand("give", "Установить благодарность, выговор, строгий выговор или завершить все")]
        public async Task GiveCommandAsync(IUser user, 
            [Choice("Благодарность", "gratitude"),
            Choice("Без статуса", "nothing"),
            Choice("Благодарность", "gratitude"),
            Choice("Выговор", "reprimand"),
            Choice("Строгий выговор", "severe-reprimand")] string statusType,
            int? amountOfDays = null)
        {
            try
            {
                StatusType? givenType;
                if (statusType == "gratitude")
                {
                    givenType = StatusType.Gratitude;
                }
                else if (statusType == "reprimand")
                {
                    givenType = StatusType.Reprimand;
                }
                else if (statusType == "severe-reprimand")
                {
                    givenType = StatusType.SevereReprimand;
                }
                else if (statusType == "nothing")
                {
                    givenType = null;
                }
                else
                {
                    throw new Exception("Ошибка: Не удалось спарсить статус!");
                }

                Unit? unit = await _db.Units.FindAsync(user.Id);

                if (unit != null)
                {
                    UnitStatus? prevUnitStatus = unit.UnitStatuses.FirstOrDefault(us =>
                    (us.Status.Type == StatusType.Gratitude
                    || us.Status.Type == StatusType.Reprimand
                    || us.Status.Type == StatusType.SevereReprimand)
                    && !us.IsCompleted());
                    if (prevUnitStatus != null)
                    {
						prevUnitStatus.EndDate = DateTime.UtcNow;
                        prevUnitStatus.RemoveRole(_guildProvider);
					}

                    if (givenType != null)
                    {
						Status? status = await _db.Statuses.FindAsync(givenType);
						if (status != null)
						{
							DateTime? endDate = amountOfDays == null ? null : DateTime.UtcNow.AddDays((double)amountOfDays);
							UnitStatus unitStatus = new UnitStatus() { Unit = unit, StartDate = DateTime.UtcNow, EndDate = endDate, Status = status };
							await _db.UnitStatuses.AddAsync(unitStatus);
							await _db.SaveChangesAsync();
							unitStatus.SetRole(_guildProvider);
							await RespondAsync(
								$"Бойцу {unit.GetOnlyNickname()} установлен(а) {status.Name}"
								+ (endDate == null ? " беcсрочно" : $" до {DateOnly.FromDateTime((DateTime)endDate).ToShortDateString()}"
								));
						}
					}
                    else
                    {
						await RespondAsync($"С бойца {unit.GetOnlyNickname()} сняты все благодарности и выговора");
						await _db.SaveChangesAsync();
					}
				}
                else
                {
                    throw new Exception("Пользователь для выдачи статуса не найден в базе данных!");
                }
            }
            catch (Exception e) 
            {
                await RespondAsync("При присвоении статуса произошла необработанная ошибка!", ephemeral: true);
                await _logService.WriteAsync(e.Message, LoggingLevel.Error);
            }
        }

        [HasPermission(PermissionType.VacationAccess)]
        [SlashCommand("vacation", "Выход в отпуск")]
        public async Task VacationCommand([MinValue(1), MaxValue(7)] int days = 7)
        {
            try
            {
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

                if (unit.UnitStatuses.Any(us => us.Status == vacationStatus && !us.IsCompleted()))
                {
					await RespondAsync("Вы уже находитесь в отпуске.", ephemeral: true);
					return;
				}

                DateTime endDate = DateTime.UtcNow.AddDays(days);
				var unitStatus = new UnitStatus()
                {
                    Unit = unit,
                    Status = vacationStatus,
                    StartDate = DateTime.UtcNow,
                    EndDate = endDate
                };

                await _db.UnitStatuses.AddAsync(unitStatus);
				await _db.SaveChangesAsync();
				unitStatus.SetRole(_guildProvider);

				await RespondAsync($"Оформлен отпуск для {unit.GetOnlyNickname()} на {days} дней до {endDate:d}.");
            }
            catch (Exception ex)
            {
                await RespondAsync("Из-за необработанной ошибки не удалось оформить отпуск.", ephemeral: true); 
                await _logService.WriteAsync(ex.Message, LoggingLevel.Error);
            }
        }


        [SlashCommand("end-vacation", "Выход из отпуска")]
        public async Task EndVacationCommand()
        {
            try
            {
                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не найдены в базе.", ephemeral: true);
                    await _logService.WriteAsync(
                        $"EndVacationCommand: Боец {Context.User.Username} с Id {Context.User.Id} не найден в бд",
                        LoggingLevel.Error
                    );
                    return;
                }

                UnitStatus? activeVacation = unit.UnitStatuses.Where(
                        us => us.Status.Type == StatusType.Vacation
                        && (us.EndDate == null || us.EndDate > DateTime.UtcNow)
                    ).FirstOrDefault();

                if (activeVacation == null)
                {
                    await RespondAsync("У вас нет активного отпуска.", ephemeral: true);
                    return;
                }

                activeVacation.EndDate = DateTime.UtcNow;
				await _db.SaveChangesAsync();
				activeVacation.RemoveRole(_guildProvider);

				await RespondAsync(
                    $"Отпуск {unit.GetOnlyNickname()} завершён досрочно."
                );
            }
            catch (Exception ex)
            {
                await RespondAsync("Не удалось завершить отпуск из-за ошибки.", ephemeral: true); 
                await _logService.WriteAsync(ex.Message, LoggingLevel.Error);
            }
        }
    }
}
