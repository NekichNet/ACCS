using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;

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

        [SlashCommand("voice", "всех бойцов в голосовом канале")]
        public async Task FixVoiceCommand([ChannelTypes(ChannelType.Voice, ChannelType.Forum)] IChannel channel)
        {
            try
            {
                List<string> ids = new List<string>();
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                await foreach (IUser user in channel.GetUsersAsync())
                {
                    Unit? unit = await _unitRepository.ReadAsync(user.Id);
                    await _logService.WriteAsync($"Voice channel user: {user.Username} unit: {unit?.Nickname}", LoggingLevel.Debug);
                    if (unit != null)
                    {
                        await _activityRepository.CreateAsync(new Activity() { Unit = unit, Date = today });
                        ids.Add(unit.DiscordId.ToString());
                    }
                }

                if (ids.Any())
                {
                    SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Редактировать список")
                        .WithCustomId($"activity-menu-{today}"); // здесь добавляем кнопки


                    ComponentBuilder builder = new ComponentBuilder();
                    builder.WithButton("Подтвердить", $"activity-verify-{today}");

                    await ReplyAsync("", components: builder.Build());
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixVoiceCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при фиксации активности по голосовому каналу");
            }
        }


        [SlashCommand("screenshot", "зафиксировать активность по скриншоту")]
        public async Task FixScreenshotCommand([ChannelTypes(ChannelType.Text, ChannelType.Forum)] IChannel channel, IAttachment screenshot)
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
                        .WithSelectMenu(menuBuilder)
                        .WithButton("Подтвердить", $"activity-verify-{today}");

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
        public async Task FixMeCommand([ChannelTypes(ChannelType.Text, ChannelType.Forum)] IChannel channel, IAttachment screenshot)
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

                await _activityRepository.CreateAsync(new Activity()
                {
                    Unit = unit,
                    Date = today
                });

                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton("Подтвердить", $"activity-verify-{today}");

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

                await _activityRepository.CreateAsync(new Activity()
                {
                    Unit = unit,
                    Date = today
                });

                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton("Подтвердить", $"activity-verify-{today}");
                string message = $"Обнаружен боец: {unit.Nickname}";
                await ReplyAsync(message, components: builder.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixUserCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при фиксации активности выбранного пользователя");
            }
        }
    }
}
