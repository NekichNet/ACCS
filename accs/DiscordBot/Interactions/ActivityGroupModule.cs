using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [InChannels("ACTIVITY_CHANNEL_ID")]
    [Group("fix", "Фиксирование активности")]
    public class ActivityGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly IGuildProviderService _guildProvider;
        private readonly IOCRService _ocr;
        private readonly ILogService _logService;

        private string? tempDir;

        public ActivityGroupModule(AppDbContext db, IGuildProviderService guildProvider, IOCRService ocr, ILogService logService)
        {
            _db = db;
            _guildProvider = guildProvider;
            _ocr = ocr;
            _logService = logService;
        }

        //[SlashCommand("voice", "Всех бойцов в голосовом канале")]
        public async Task FixVoiceCommand([ChannelTypes(ChannelType.Voice, ChannelType.Forum)] IChannel channel)
        {
            try
            {
                List<Unit> units = new List<Unit>();
                IEnumerable<IUser> users = await channel.GetUsersAsync().FlattenAsync();

				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                foreach (IUser user in users)
                {
                    Unit? unit = await _db.Units.FindAsync(user.Id);
                    if (unit != null)
                    {
                        units.Add(unit);
                    }
                }

                if (units.Any())
                {
                    SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Редактировать список")
                        .WithCustomId($"activity-select-menu")
                        .WithMinValues(0)
                        .WithMaxValues(units.Count);

                    foreach (var unit in units)
                    {
                        menuBuilder.AddOption(unit.Nickname, unit.DiscordId.ToString(), isDefault: true);
                    }

                    ComponentBuilder builder = new ComponentBuilder();
                    builder.WithSelectMenu(menuBuilder)
						.WithButton("Подтвердить", customId: $"activity-menu-{today}", ButtonStyle.Success);

                    await RespondAsync($"Подтвердите активность для {units.Count} бойцов.", components: builder.Build());
                }
                else
                {
                    await RespondAsync("Бойцы не найдены");
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixVoiceCommand: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при фиксации активности по голосовому каналу");
            }
        }

        [SlashCommand("screenshot", "зафиксировать активность по скриншоту")]
        public async Task FixScreenshotCommand(IAttachment screenshot)
        {
            await DeferAsync();

            try
            {
                if (screenshot == null || screenshot.ContentType == null || !screenshot.ContentType.StartsWith("image"))
                {
                    await FollowupAsync("Скриншот не найден или не является изображением");
                    return;
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                /* OCR */
                string tempDir = Path.Combine(Path.GetTempPath(), "temp");
                Directory.CreateDirectory(tempDir);

                string filePath = Path.Combine(tempDir, screenshot.Filename);
                using (var http = new HttpClient())
                {
                    var bytes = await http.GetByteArrayAsync(screenshot.Url);
                    await File.WriteAllBytesAsync(filePath, bytes);
                }

                HashSet<Unit> detectedUnits = await _ocr.ReceiveNamesFromPhoto(filePath);

                if (detectedUnits.Any())
                {
                    SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Редактировать список")
                        .WithCustomId($"activity-menu-{today}")
                        .WithMinValues(0)
                        .WithMaxValues(detectedUnits.Count);

                    foreach (var unit in detectedUnits)
                    {
                        menuBuilder.AddOption(unit.Nickname, unit.DiscordId.ToString(), isDefault: true);
                    }

                    ComponentBuilder builder = new ComponentBuilder()
                        .WithSelectMenu(menuBuilder);

                    string message = $"Подтвердите активность для {detectedUnits.Count} бойцов.";
                    await ReplyAsync(message, components: builder.Build());
                }
                else
                {
                    await ReplyAsync("Бойцы не найдены на скриншоте");
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixScreenshotCommand: {ex.Message}", LoggingLevel.Error);
                await ReplyAsync("Ошибка при обработке скриншота");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [SlashCommand("user", "зафиксировать активность указанного бойца")]
        public async Task FixUserCommand(IUser? user = null)
        {
            try
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                Unit? unit;
                if (user != null)
                {
                    unit = await _db.Units.FindAsync(user.Id);
                }
                else
                {
                    unit = await _db.Units.FindAsync(Context.User.Id);
                }

                if (unit == null)
                {
                    await ReplyAsync("Боец не найден в системе");
                    return;
                }

                ComponentBuilder builder = new ComponentBuilder();

				Dictionary<Unit, bool> dict = new Dictionary<Unit, bool> { { unit, unit.Activities.Any(a => a.Date == today) } };

                if (dict.Any(p => !p.Value))
                {
                    builder.WithButton("Подтвердить", customId: $"confirm-activity-{today}-{Context.User.Id}:{string.Join(',', unit.DiscordId)}", ButtonStyle.Success);
				}

				EmbedBuilder embedBuilder = GetResultsEmbedBuilder(dict, today)
                    .WithAuthor((await _db.Units.FindAsync(Context.User.Id)).Nickname, Context.User.GetDisplayAvatarUrl());

                await RespondAsync(components: builder.Build(), embed: embedBuilder.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixUserCommand: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при фиксации активности пользователя", ephemeral: true);
            }
        }

        /*
		[HasPermission(PermissionType.ConfirmActivity)]
		[ComponentInteraction("activity-select-menu", ignoreGroupNames: true)]
        public async Task SelectMenuHandler(string[] selectedIds)
        {
			((SelectMenuComponent)(await GetOriginalResponseAsync()).Components.First()).CustomId = String.Join(',', selectedIds);

		}
        */

        /*
		[HasPermission(PermissionType.ConfirmActivity)]
        [ComponentInteraction("activity-verify-*-*", ignoreGroupNames: true)]
        public async Task VerifyActivityHandler(string dateRaw, string unitIdRaw)
        {
            Console.WriteLine(dateRaw + " " + unitIdRaw);
            try
            {
                if (!DateOnly.TryParse(dateRaw, out DateOnly date))
                {
                    await RespondAsync("Ошибка: неверный формат даты", ephemeral: true);
                    return;
                }

                if (ulong.TryParse(unitIdRaw, out ulong unitId))
                {
                    Unit? unit = await _db.Units.FindAsync(unitId);

                    if (unit != null)
                    {
						unit.RankUpCounter++;
						await CheckRankUpCounterAsync(unit);
						await _db.Activities.AddAsync(new Activity()
                        {
                            Unit = unit,
                            Date = date
                        });
                        await ModifyOriginalResponseAsync(properties =>
                        {
                            properties.Content = $"Подтверждена активность для: {unit.GetOnlyNickname()}";
                            properties.Components = null;
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
                    await RespondAsync("Ошибка: неверный формат ID бойца", ephemeral: true);
                    await _logService.WriteAsync("Ошибка: неверный формат ID бойца", LoggingLevel.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in VerifyActivity: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при подтверждении активности", ephemeral: true);
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }
        */

        [HasPermission(PermissionType.ConfirmActivity)]
        [ComponentInteraction("confirm-activity-*-*:*", ignoreGroupNames: true)]
        public async Task ActivityMenuHandler(string dateString, string authorIdString, string idsString)
        {
            try
            {
				if (!DateOnly.TryParse(dateString, out DateOnly date))
                {
                    await RespondAsync("Ошибка: неверный формат даты", ephemeral: true);
					await _logService.WriteAsync("Неверный формат даты", LoggingLevel.Error);
					return;
                }

                if (!ulong.TryParse(authorIdString, out ulong authorId))
                {
					await RespondAsync("Ошибка: не удалось получить автора запроса на фиксацию", ephemeral: true);
					await _logService.WriteAsync("Не удалось получить автора запроса на фиксацию", LoggingLevel.Error);
					return;
				}

                if (idsString.Length < 2)
                {
					await RespondAsync("Ошибка: не удалось получить список бойцов на фиксацию", ephemeral: true);
					await _logService.WriteAsync("Не удалось получить список бойцов на фиксацию", LoggingLevel.Error);
					return;
				}

                List<ulong> ids = idsString.Split(',').Select(i => ulong.Parse(i)).ToList();

				Dictionary<Unit, bool> units = new Dictionary<Unit, bool>();

				foreach (ulong id in ids)
				{
					Unit? unit = await _db.Units.FindAsync(id);

					if (unit != null)
					{
                        if (!unit.Activities.Any(a => a.Date == date))
                        {
							unit.RankUpCounter++;
							await CheckRankUpCounterAsync(unit);
							await _db.Activities.AddAsync(new Activity()
							{
								Unit = unit,
								Date = date
							});
							await _db.SaveChangesAsync();
						}
						units.Add(unit, true);
					}
				}

                Unit? author = await _db.Units.FindAsync(authorId);
                Unit? confirmator = await _db.Units.FindAsync(Context.User.Id);

				EmbedBuilder embed = GetResultsEmbedBuilder(units, date);

                if (author != null)
                    embed.WithAuthor(name: author.Nickname, iconUrl: _guildProvider.GetGuild().GetUser(authorId).GetDisplayAvatarUrl());

                if (confirmator != null)
                    embed.WithFooter(new EmbedFooterBuilder().WithText($"Подтверждено {confirmator.Nickname}"));
                else
					embed.WithFooter(new EmbedFooterBuilder().WithText($"Подтверждено {Context.User.Username}"));

				await RespondAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in ActivityMenuHandler: {ex.StackTrace}", LoggingLevel.Error);
                await RespondAsync("Ошибка при подтверждении списка бойцов", ephemeral: true);
            }
        }

        private EmbedBuilder GetResultsEmbedBuilder(Dictionary<Unit, bool> units, DateOnly date)
        {
            string unitsString = "";
            ushort unconfirmedCounter = 0;
            foreach (KeyValuePair<Unit, bool> pair in units)
            {
				unitsString += "\r\n" + (pair.Value ? ":white_check_mark: [" : ":black_medium_square: [") + pair.Key.DiscordId + "] " + pair.Key.Nickname;
                unconfirmedCounter += pair.Value ? (ushort)0 : (ushort)1;
			}

            if (units.Count == 0)
                unitsString = "\r\nБойцов для фиксации не обнаружено";

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Фиксация активности")
                .AddField("Дата:", date.ToShortDateString(), inline: true)
                .AddField("Количество:", units.Count, inline: true)
                .AddField("Бойцы:", unitsString)
                .WithColor(unconfirmedCounter > 0 ? Color.Red : Color.DarkGreen);

            return embed;
        }

        private async Task CheckRankUpCounterAsync(Unit unit)
        {
            if (unit.Rank.Next != null)
            {
                if (unit.Rank.Next.CounterToReach <= unit.RankUpCounter)
                {
                    string channelIdString = DotNetEnv.Env.GetString("NOTIFICATION_CHANNEL_ID", "NOTIFICATION_CHANNEL_ID is not found!");
                    if (ulong.TryParse(channelIdString, out ulong channelId))
						await _guildProvider.GetGuild().GetTextChannel(channelId).SendMessageAsync($"Нужно повысить бойца {unit.Nickname}: {unit.RankUpCounter}/{unit.Rank.Next.CounterToReach}.");
                    else
						await _logService.WriteAsync("Не удалось спарсить NOTIFICATION_CHANNEL_ID!", LoggingLevel.Error);
				}
            }
        }
    }
}
