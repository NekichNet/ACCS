using accs.Database;
using accs.DiscordBot.Interactions.Models;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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

        public ActivityGroupModule(AppDbContext db, IGuildProviderService guildProvider, IOCRService ocr, ILogService logService)
        {
            _db = db;
            _guildProvider = guildProvider;
            _ocr = ocr;
            _logService = logService;
        }

        [SlashCommand("voice", "Зафиксировать активность всех бойцов в голосовом канале")]
        public async Task FixVoiceCommand([ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IChannel channel)
        {
            try
            {
                IEnumerable<SocketGuildUser> users;
				Dictionary<Unit, bool> units = new Dictionary<Unit, bool>();
                if (channel.ChannelType == ChannelType.Stage)
					users = ((SocketStageChannel)channel).ConnectedUsers;
                else
					users = ((SocketVoiceChannel)channel).ConnectedUsers;

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                foreach (IUser user in users)
                {
                    Unit? unit = await _db.Units.FindAsync(user.Id);
                    if (unit != null)
                        units.Add(unit, unit.Activities.Any(a => a.Date == today));
                }

                if (units.Any())
                {
                    ComponentBuilder component = new ComponentBuilder();

					if (units.Any(p => !p.Value))
                    {
						string customId = $"confirm-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds}";
						await SaveActivityJson(customId, Context.User.Id, units);

						component.WithButton("Подтвердить", customId: customId, ButtonStyle.Success);
                    }

					EmbedBuilder embedBuilder = GetResultsEmbedBuilder(units, today)
						.WithAuthor((await _db.Units.FindAsync(Context.User.Id)).Nickname, Context.User.GetDisplayAvatarUrl());

					await RespondAsync(components: component.Build(), embed: embedBuilder.Build());
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

        [SlashCommand("screenshot", "Зафиксировать активность всех бойцов на скриншоте")]
        public async Task FixScreenshotCommand(IAttachment screenshot)
        {
            await DeferAsync(ephemeral: true);

            try
            {
                if (screenshot == null || screenshot.ContentType == null || !screenshot.ContentType.StartsWith("image"))
                {
					await ModifyOriginalResponseAsync((props) => { props.Content = "Неправильный формат файла"; });
					return;
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                /* OCR */
                if (!Directory.Exists("temp"))
                    Directory.CreateDirectory("temp");

                string filePath = Path.Join("temp", screenshot.Filename);
                using (var http = new HttpClient())
                {
                    var bytes = await http.GetByteArrayAsync(screenshot.Url);
                    await File.WriteAllBytesAsync(filePath, bytes);
                }

                HashSet<Unit> detectedUnits = await _ocr.ReceiveNamesFromPhoto(filePath);
                Dictionary<Unit, bool> units = new Dictionary<Unit, bool>();

                await _db.Activities.LoadAsync();
                foreach (Unit unit in detectedUnits)
                    units.Add(unit, unit.Activities.Any(a => a.Date == today));

                if (units.Any())
                {
                    string customId = $"confirm-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds}";
                    await SaveActivityJson(customId, Context.User.Id, units);

                    ComponentBuilder component = new ComponentBuilder();

					if (units.Any(p => !p.Value))
						component.WithButton("Подтвердить", customId: customId, ButtonStyle.Success);

					EmbedBuilder embedBuilder = GetResultsEmbedBuilder(units, today)
						.WithAuthor((await _db.Units.FindAsync(Context.User.Id)).Nickname, Context.User.GetDisplayAvatarUrl());

                    await DeleteOriginalResponseAsync();
                    await ReplyAsync(components: component.Build(), embed: embedBuilder.Build());
                }
                else
                {
					await ModifyOriginalResponseAsync((props) => { props.Content = "Бойцы не найдены на скриншоте"; });
				}
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in FixScreenshotCommand: {ex.Message}", LoggingLevel.Error);
				await ModifyOriginalResponseAsync((props) => { props.Content = "Произошла непредвиденная ошибка"; });
			}
        }

        [SlashCommand("user", "Зафиксировать активность указанного бойца")]
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

				Dictionary<Unit, bool> dict = new Dictionary<Unit, bool> 
                { 
                    { unit, unit.Activities.Any(a => a.Date == today) } 
                };

				string customId = $"confirm-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds}";
				await SaveActivityJson(customId, Context.User.Id, dict);

				if (dict.Any(p => !p.Value))
                {
                    builder.WithButton("Подтвердить", customId: customId, ButtonStyle.Success);
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

        [HasPermission(PermissionType.ConfirmActivity)]
        [ComponentInteraction("confirm-*", ignoreGroupNames: true)]
        public async Task ConfirmActivityHandler(string unixString)
        {
            try
            {
                string customId = $"confirm-{unixString}";
                DateOnly date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixString)).DateTime);

                UnconfirmedActivity? unconfirmedActivity;
				using (FileStream fs = new FileStream(Path.Join("temp", $"{customId}.json"), FileMode.OpenOrCreate))
				{
					unconfirmedActivity = await System.Text.Json.JsonSerializer.DeserializeAsync<UnconfirmedActivity>(fs);
				}

				Dictionary<Unit, bool> units = new Dictionary<Unit, bool>();
                
                if (unconfirmedActivity == null)
                {
                    await RespondAsync("Ошибка: не удалось прочитать файл запроса фиксации.", ephemeral: true);
                    await _logService.WriteAsync("Ошибка: не удалось прочитать файл запроса фиксации.", LoggingLevel.Error);
                    return;
                }

				foreach (ulong id in unconfirmedActivity.UnitIds)
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

                await ((IComponentInteraction)Context.Interaction).Message.DeleteAsync();

                Unit? author = await _db.Units.FindAsync(authorId);
                Unit? confirmator = await _db.Units.FindAsync(Context.User.Id);

				EmbedBuilder embed = GetResultsEmbedBuilder(units, date);

                if (author != null)
                    embed.WithAuthor(name: author.Nickname, iconUrl: _guildProvider.GetGuild().GetUser(authorId).GetDisplayAvatarUrl());

                if (confirmator != null)
                    embed.WithFooter(new EmbedFooterBuilder().WithText($"Подтверждено {confirmator.Rank.Name} {confirmator.Nickname}"));
                else
					embed.WithFooter(new EmbedFooterBuilder().WithText($"Подтверждено {Context.User.Username}"));

				await RespondAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in ConfirmActivityHandler: {ex.StackTrace}", LoggingLevel.Error);
                await RespondAsync("Ошибка при подтверждении списка бойцов", ephemeral: true);
            }
        }

        private EmbedBuilder GetResultsEmbedBuilder(Dictionary<Unit, bool> units, DateOnly date)
        {
            string unitsString = "";
            ushort unconfirmedCounter = 0;
            foreach (KeyValuePair<Unit, bool> pair in units)
            {
				unitsString += "\r\n" + (pair.Value ? ":white_check_mark: " : ":black_medium_square: ")
                    + pair.Key.Rank.Name + ' ' + pair.Key.Nickname;
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

        private async Task SaveActivityJson(string customId, ulong authorId, Dictionary<Unit, bool> units)
        {
            try
            {
                string dir = "temp";

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Join(dir, $"{customId}.json");

                UnconfirmedActivity activity = new UnconfirmedActivity
                {
                    UnitIds = units.Keys.Select(u => u.DiscordId).ToList(),
                    AuthorId = authorId,
                };

                string json = System.Text.Json.JsonSerializer.Serialize(
                    activity,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );

                await File.WriteAllTextAsync(filePath, json);

                await _logService.WriteAsync(
                    $"[Activity JSON] Создан файл {filePath} для customId '{customId}'",
                    LoggingLevel.Info
                );
            }
            catch (Exception ex)
            {
                await RespondAsync("Ошибка при создании JSON файла активности", ephemeral: true);
                await _logService.WriteAsync($"Ошибка при создании JSON файла активности: {ex.Message}", LoggingLevel.Error);
            }
        }
    }
}
