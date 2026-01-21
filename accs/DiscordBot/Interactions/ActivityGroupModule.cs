using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [InChannels("ACTIVITY_CHANNEL_ID")]
    [Group("fix", "Фиксирование активности")]
    public class ActivityGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IActivityRepository _activityRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;

        public ActivityGroupModule(IActivityRepository activityRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _activityRepository = activityRepository;
            _unitRepository = unitRepository;
            _logService = logService;
        }

        [HasPermission(PermissionType.ConfirmActivity)]
        [SlashCommand("voice", "Всех бойцов в голосовом канале")]
        public async Task FixVoiceCommand([ChannelTypes(ChannelType.Voice, ChannelType.Forum)] IChannel channel)
        {
            try
            {
                List<Unit> units = new List<Unit>();
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                await foreach (IUser user in channel.GetUsersAsync())
                {
                    Unit? unit = await _unitRepository.ReadAsync(user.Id);
                    if (unit != null)
                    {
                        units.Add(unit);
                    }
                }

                if (units.Any())
                {
                    SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Редактировать список")
                        .WithCustomId($"activity-menu-{today}")
                        .WithMinValues(0)
                        .WithMaxValues(units.Count);

                    foreach (var unit in units)
                    {
                        menuBuilder.AddOption(unit.Nickname, unit.DiscordId.ToString());
                    }

                    ComponentBuilder builder = new ComponentBuilder();
                    builder.WithSelectMenu(menuBuilder);

                    await ReplyAsync("Требуется подтверждение", components: builder.Build());
                }
                else
                {
                    await ReplyAsync("Бойцы не найдены");
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixVoiceCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при фиксации активности по голосовому каналу");
            }
        }


        [HasPermission(PermissionType.ConfirmActivity)]
        [SlashCommand("screenshot", "зафиксировать активность по скриншоту")]
        public async Task FixScreenshotCommand(IAttachment screenshot)
        {
            await DeferAsync();  // это шоб команда не таймаутилась при долгой обработке

            try
            {
                if (screenshot == null || screenshot.ContentType == null || !screenshot.ContentType.StartsWith("image"))
                {
                    await FollowupAsync("Скриншот не найден или не является изображением");
                    return;
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                /* тут питоновский скрипт скидывает сюда список юнитов, 
                 * которых он смог распознать и мы слизываем их в detectedUnits */

                List<Unit> detectedUnits = new List<Unit>();

                if (detectedUnits.Any())
                {
                    SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Редактировать список")
                        .WithCustomId($"activity-menu-{today}")
                        .WithMinValues(0)
                        .WithMaxValues(detectedUnits.Count);

                    foreach (var unit in detectedUnits)
                    {
                        menuBuilder.AddOption(unit.Nickname, unit.DiscordId.ToString());
                    }

                    ComponentBuilder builder = new ComponentBuilder()
                        .WithSelectMenu(menuBuilder);

                    string message = $"Обнаружено бойцов: {detectedUnits.Count}\n";
                    await FollowupAsync(message, components: builder.Build());
                }
                else
                {
                    await FollowupAsync("Бойцы не найдены на скриншоте");
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixScreenshotCommand: {ex.Message}", LoggingLevel.Error);
                await FollowupAsync("Ошибка при обработке скриншота");
            }
        }


        [SlashCommand("me", "зафиксировать свою активность")]
        public async Task FixMeCommand()
        {
            try
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                Unit? unit = await _unitRepository.ReadAsync(Context.User.Id);

                if (unit == null)
                {
                    await ReplyAsync("Вы не найдены в системе");
                    return;
                }


                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton("Подтвердить", $"activity-verify-{today}-{unit.DiscordId}");

                string message = $"Обнаружен боец: {unit.Nickname}";

                await ReplyAsync(message, components: builder.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixMeCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при фиксации своей активности");
            }
        }


        [SlashCommand("user", "зафиксировать активность указанного бойца")]
        public async Task FixUserCommand(IUser user)
        {
            try
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                Unit? unit = await _unitRepository.ReadAsync(user.Id);

                if (unit == null)
                {
                    await ReplyAsync("Боец не найден в системе");
                    return;
                }

                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton("Подтвердить", $"activity-verify-{today}-{unit.DiscordId}");

                string message = $"Обнаружен боец: {unit.Nickname}";
                await ReplyAsync(message, components: builder.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixUserCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при фиксации активности выбранного пользователя");
            }
        }


        [HasPermission(PermissionType.ConfirmActivity)]
        [ComponentInteraction("activity-verify-*-*")]
        public async Task VerifyActivityHandler(string dateRaw, string unitIdRaw)
        {
            try
            {
                if (!DateOnly.TryParse(dateRaw, out DateOnly date))
                {
                    await RespondAsync("Ошибка: неверный формат даты", ephemeral: true);
                    return;
                }

                var component = (SocketMessageComponent)Context.Interaction;
                var selectedIds = component.Data.Values;

                if (component == null)
                {
                    await RespondAsync("Ошибка взаимодействия", ephemeral: true);
                    return;
                }


                if (ulong.TryParse(unitIdRaw, out ulong unitId))
                {
                    Unit? unit = await _unitRepository.ReadAsync(unitId);

                    if (unit != null)
                    {
                        await _activityRepository.CreateAsync(new Activity()
                        {
                            Unit = unit,
                            Date = date
                        });
                    }
                    else
                    {
                        await RespondAsync("Боец не найден", ephemeral: true);
                        return;
                    }
                }
                else
                {
                    await RespondAsync("Ошибка: неверный формат ID бойца");
                    await _logService.WriteAsync("Ошибка: неверный формат ID бойца", LoggingLevel.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in VerifyActivity: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при подтверждении активности", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.ConfirmActivity)]
        [ComponentInteraction("activity-menu-*")]
        public async Task ActivityMenuHandler(string dateRaw)
        {
            try
            {
                if (!DateOnly.TryParse(dateRaw, out DateOnly date))
                {
                    await RespondAsync("Ошибка: неверный формат даты", ephemeral: true);
                    return;
                }

                var component = (SocketMessageComponent)Context.Interaction;

                var selectedIds = component.Data.Values;

                if (selectedIds == null || !selectedIds.Any())
                {
                    await RespondAsync("Вы не выбрали ни одного бойца", ephemeral: true);
                    return;
                }

                int count = 0;

                foreach (var idStr in selectedIds)
                {
                    if (ulong.TryParse(idStr, out ulong id))
                    {
                        Unit? unit = await _unitRepository.ReadAsync(id);

                        if (unit != null)
                        {
                            await _activityRepository.CreateAsync(new Activity()
                            {
                                Unit = unit,
                                Date = date
                            });

                            count++;
                        }
                    }
                }

                await RespondAsync($"Активность за {date} подтверждена для {count} бойцов.");

            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in ActivityMenuHandler: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при обновлении списка", ephemeral: true);
            }
        }
    }
}
