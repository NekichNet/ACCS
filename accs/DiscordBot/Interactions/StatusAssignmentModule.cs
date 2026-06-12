using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    public class StatusAssignmentModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StatusAssignmentModule> _log;
        private readonly IGuildProviderService _guildProvider;

        public StatusAssignmentModule(AppDbContext db, ILogger<StatusAssignmentModule> log, IGuildProviderService guildProvider)
        {
            _db = db;
            _log = log;
            _guildProvider = guildProvider;
        }

        [HasPermission(PermissionType.GiveReprimandGratitude)]
        [SlashCommand("give", "Выдать благодарность, выговор или строгий выговор")]
        public async Task GiveCommandAsync(
            [Summary(description: "Боец, которому Вы присваиваете статус")]
            IUser user,
            [Choice("Благодарность", "gratitude"),
            Choice("Выговор", "reprimand"),
            Choice("Строгий выговор", "severe-reprimand")]
            [Summary(description: "Вид статуса")]
            string statusType,
            [Summary(description: "Количество дней")]
            int? amountOfDays = 7)
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
                else if (statusType == "severe-reprimand")
                {
                    givenType = StatusType.SevereReprimand;
                }
                else
                {
                    throw new Exception("Ошибка: Не удалось спарсить статус!");
                }

				Status status = await _db.Statuses.FindAsync(givenType);
				Unit? unit = await _db.Units.FindAsync(user.Id);

                if (unit != null)
                {
                    StatusType finalStatusType;

                    UnitStatus? prevUnitStatus = unit.UnitStatuses.FirstOrDefault(us =>
                    (us.Status.Type == StatusType.Gratitude
                    || us.Status.Type == StatusType.Reprimand
                    || us.Status.Type == StatusType.SevereReprimand)
                    && !us.IsCompleted());
                    if (prevUnitStatus != null)
                    {
                        Dictionary<StatusType, int> statusMath = new Dictionary<StatusType, int>();
                        
                        statusMath.Add(StatusType.Gratitude, 1);
						statusMath.Add(StatusType.Reprimand, -1);
						statusMath.Add(StatusType.SevereReprimand, -2);

						prevUnitStatus.EndDate = DateTime.UtcNow;
                        prevUnitStatus.RemoveRole(_guildProvider);

                        int sum = statusMath[prevUnitStatus.Status.Type] + statusMath[givenType];

                        if (sum > 0)
                            finalStatusType = StatusType.Gratitude;
                        else if (sum == -1)
                            finalStatusType = StatusType.Reprimand;
                        else if (sum < -1)
                            finalStatusType = StatusType.SevereReprimand;
                        else
                        {
							await RespondAsync($"Бойцу {unit.GetOnlyNickname()} выдан(а) {status.Name}. Текущий статус: отсутствует");
                            return;
						}
                    }
                    else
                    {
                        finalStatusType = givenType;
                    }

                    DateTime endDate = DateTime.UtcNow.AddDays((double)amountOfDays);
                    Status finalStatus = await _db.Statuses.FindAsync(finalStatusType);
					UnitStatus unitStatus = new UnitStatus() { Unit = unit, StartDate = DateTime.UtcNow, EndDate = endDate, Status = finalStatus };
                    await _db.UnitStatuses.AddAsync(unitStatus);
                    await _db.SaveChangesAsync();
                    unitStatus.SetRole(_guildProvider);
                    await RespondAsync(
                        $"Бойцу {unit.GetOnlyNickname()} выдан(а) {status.Name}. " +
                        $"Текущий статус: {finalStatus.Name} до {DateOnly.FromDateTime((DateTime)endDate).ToShortDateString()}"
                        );
                }
                else
                {
                    throw new Exception("Пользователь для выдачи статуса не найден в базе данных!");
                }
            }
            catch (Exception e)
            {
                await RespondAsync("При присвоении статуса произошла необработанная ошибка!", ephemeral: true);
                _log.LogError(e, e.Message);
            }
        }

		[InChannels("ACTIVITY_CHANNEL_ID")]
		[HasPermission(PermissionType.VacationAccess)]
        [SlashCommand("vacation", "Выход в отпуск")]
        public async Task VacationCommand(
            [MinValue(1), MaxValue(7)]
            [Summary(description: "Длительность в днях. 7 максимально. По умолчанию 7")]
            int days = 7)
        {
            try
            {
                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не найдены в системе.", ephemeral: true);
					_log.LogError($"VacationCommandAsync: Боец {Context.User.Username} с Id {Context.User.Id} не найден в базе");
                    return;
                }

                Status? vacationStatus = await _db.Statuses.FindAsync(StatusType.Vacation);
                if (vacationStatus == null)
                {
                    await RespondAsync("Статус 'Отпуск' не найден в базе.", ephemeral: true);
					_log.LogError($"VacationCommandAsync: Статус 'Отпуск' не найден в базе.");
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

                await RespondAsync($"Оформлен отпуск для {unit.GetOnlyNickname()} на {days} дней до {endDate:g}.");
            }
            catch (Exception ex)
            {
                await RespondAsync("Из-за необработанной ошибки не удалось оформить отпуск.", ephemeral: true);
				_log.LogError(ex, ex.Message);
            }
        }

		[InChannels("ACTIVITY_CHANNEL_ID")]
		[SlashCommand("end-vacation", "Выход из отпуска")]
        public async Task EndVacationCommand()
        {
            try
            {
                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не найдены в базе.", ephemeral: true);
					_log.LogError($"EndVacationCommand: Боец {Context.User.Username} с Id {Context.User.Id} не найден в бд");
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
				_log.LogError(ex.Message);
            }
        }

        [HasPermission(PermissionType.Administrator)]
        [SlashCommand("retirement-user", "Отправить человека в отставку")]
        public async Task RetirementUserCommand(
            [Summary(description: "Боец, которого Вы отправляете в отставку")]
            IUser user)
        {
            await DeferAsync();

            Unit? unit = await _db.Units.FindAsync(user.Id);

            if (unit != null)
            {
                if (!unit.UnitStatuses.Any(us => us.Status.Type == StatusType.Retirement && !us.IsCompleted()))
                {
                    Status? retirement = await _db.Statuses.FindAsync(StatusType.Retirement);
                    if (retirement == null)
                    {
                        await ModifyOriginalResponseAsync(r => r.Content = "Ошибка: не удалось получить статус отставки!");
                        return;
                    }

                    unit.UnitStatuses.Add(new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = retirement });
                    unit.Posts.Clear();

                    _db.SaveChanges();

                    SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(user.Id);
                    if (guildUser != null)
                    {
                        await guildUser.RemoveRolesAsync(guildUser.Roles);
                        if (retirement.DiscordRoleId != null)
                            await guildUser.AddRoleAsync((ulong)retirement.DiscordRoleId);
                        await ModifyOriginalResponseAsync(r => r.Content = $"Отставка {unit.GetOnlyNickname()} оформлена.");
                    }
                    else
                        await ModifyOriginalResponseAsync(r => r.Content = $"Отставка {unit.GetOnlyNickname()} оформлена, но не удалось выдать роли.");
                }
                else
                    await ModifyOriginalResponseAsync(r => r.Content = "Пользователь уже в отставке.");
            }
            else
                await ModifyOriginalResponseAsync(r => r.Content = "Пользователя нет в системе.");
        }

        [HasPermission(PermissionType.Administrator)]
        [SlashCommand("retirement-id", "Отправить человека в отставку по ID пользователя.")]
        public async Task RetirementIdCommand(
            [Summary(name: "user-id", description: "Discord ID бойца, которого Вы отправляете в отставку")]
            string idString)
        {
            await DeferAsync();

            ulong id;
            if (ulong.TryParse(idString, out id))
            {
                Unit? unit = await _db.Units.FindAsync(id);

                if (unit != null)
                {
                    if (!unit.UnitStatuses.Any(us => us.Status.Type == StatusType.Retirement && !us.IsCompleted()))
                    {
                        Status? retirement = await _db.Statuses.FindAsync(StatusType.Retirement);
                        if (retirement == null)
                        {
                            await ModifyOriginalResponseAsync(r => r.Content = "Ошибка: не удалось получить статус отставки!");
                            return;
                        }

                        SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(id);
                        if (guildUser != null)
                        {
                            foreach (Post post in unit.Posts)
                            {
                                List<IRole> roles = new List<IRole>();
                                if (post.DiscordRoleId != null)
                                    roles.Add(await Context.Guild.GetRoleAsync((ulong)post.DiscordRoleId));
                                Subdivision? subdiv = post.Subdivision;
                                while (subdiv != null)
                                {
                                    if (subdiv.DiscordRoleId != null)
                                        roles.Add(await Context.Guild.GetRoleAsync((ulong)subdiv.DiscordRoleId));
                                    subdiv = subdiv.Head;
                                }

                                await guildUser.RemoveRolesAsync(roles);
                            }

                            if (unit.Rank.DiscordRoleId != null)
                                await guildUser.RemoveRoleAsync((ulong)unit.Rank.DiscordRoleId);
                            await RespondAsync($"{unit.GetOnlyNickname()} был уволен.");

                            unit.UnitStatuses.Add(new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = retirement });
                            unit.Posts.Clear();

                            _db.SaveChanges();
                        }

                    }
                    else
                        await ModifyOriginalResponseAsync(r => r.Content = "Пользователь уже в отставке.");
                }
                else
                    await ModifyOriginalResponseAsync(r => r.Content = "Пользователя нет в системе.");
            }
        }

        [HasPermission(PermissionType.ChangePosts)]
        [SlashCommand("temp-post", "Выдать или завершить статус ВрИО.")]
        public async Task TempPostCommand(
            [Summary(description: "Боец, которому Вы выдаёте статус ВрИО")]
            IUser user)
        {
            await DeferAsync();

            Unit? unit = await _db.Units.FindAsync(user.Id);

			Status? temporaryPost = await _db.Statuses.FindAsync(StatusType.TemporaryPost);
			if (temporaryPost == null)
			{
				await ModifyOriginalResponseAsync(r => r.Content = "Ошибка: не удалось получить статус ВрИО!");
				return;
			}

			if (unit != null)
            {
                if (!unit.UnitStatuses.Any(us => us.Status.Type == StatusType.TemporaryPost && !us.IsCompleted()))
                {
                    unit.UnitStatuses.Add(new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = temporaryPost });

                    _db.SaveChanges();

                    SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(user.Id);
                    if (guildUser != null)
                    {
                        if (temporaryPost.DiscordRoleId != null)
                            await guildUser.AddRoleAsync((ulong)temporaryPost.DiscordRoleId);
						await ModifyOriginalResponseAsync(r => r.Content = $"{unit.GetOnlyNickname()} выдан статус ВрИО.");
					}
                    else
                        await ModifyOriginalResponseAsync(r => r.Content = $"{unit.GetOnlyNickname()} выдан статус ВрИО, но не удалось выдать роль.");
                }
                else
                {
                    UnitStatus? tempPost = unit.UnitStatuses.Find(us => us.Status.Type == StatusType.TemporaryPost && !us.IsCompleted());
                    if (tempPost != null)
                    {
                        tempPost.EndDate = DateTime.UtcNow;

                        SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(user.Id);
						if (guildUser != null)
						{
							if (temporaryPost.DiscordRoleId != null)
								await guildUser.RemoveRoleAsync((ulong)temporaryPost.DiscordRoleId);
							await ModifyOriginalResponseAsync(r => r.Content = $"{unit.GetOnlyNickname()} завершён статус ВрИО.");
						}
						else
							await ModifyOriginalResponseAsync(r => r.Content = $"{unit.GetOnlyNickname()} завершён статус ВрИО, но не удалось убрать роль.");
					}
                }
            }
            else
                await ModifyOriginalResponseAsync(r => r.Content = "Пользователя нет в системе.");
        }
    }
}
