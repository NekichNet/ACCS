using accs.Database;
using discord_bot.Preconditions;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using accs.Models;

namespace discord_bot.Interactions
{
    public class ProfileGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ProfileGroupModule> _log;
        private readonly IGuildProviderService _guildProvider;
        
        public ProfileGroupModule(AppDbContext db, ILogger<ProfileGroupModule> log, IGuildProviderService guildProvider) 
        {
            _db = db;
            _log = log;
            _guildProvider = guildProvider;
        }

		[IsUnit()]
		[SlashCommand("profile", "Показать профиль указанного пользователя")]
        public async Task ShowProfileCommand(IUser? user = null)
        {
            await DeferAsync();

            Unit? unit;
			_log.LogDebug($"user: {user}");
			if (user == null) { unit = await _db.Units.FindAsync(Context.User.Id); }
            else
            {
				_log.LogDebug($"user id: {user.Id}");
				unit = await _db.Units.FindAsync(user.Id);
            }
			_log.LogDebug($"unit: {unit}");

			if (unit != null)
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"{unit.Rank.Name} {unit.GetOnlyNickname()}",
                    Description = string.Join("\n", unit.Posts.Select(p => p.GetFullName()))
                };

                string inLineUnitActivities = string.Empty;
                for (int i = -27; i <= 0; i++)
                {
                    if(unit.Activities.Any(a => a.Date == DateOnly.FromDateTime(DateTime.Today).AddDays(i)))
                        inLineUnitActivities += ":green_square:";
                    else
                        inLineUnitActivities += ":black_medium_square:";
                    if (i == -14)
                        inLineUnitActivities += '\n';
                }
                
                if (unit.UnitStatuses.Any(us => !us.IsCompleted()))
                    embed.AddField(new EmbedFieldBuilder() {
                        Name = "Статусы:", Value = "```ansi\r\n" + string.Join(", ",
                        unit.UnitStatuses.Where(us => !us.IsCompleted()).Select(us => us.Status.Name)) + "\r\n```"
					});
                if (unit.Rewards.Any())
                    embed.AddField(new EmbedFieldBuilder() {
                        Name = "Награды:", Value = "```ansi\r\n\u001b[2;33m" + string.Join(", ",
                        unit.Rewards.Select(r => r.Name)) + "\u001b[0m\r\n```"
                    });
                embed.AddField(new EmbedFieldBuilder() {
                    Name = "Благодарности:", Value = unit.UnitStatuses.Where(
                        x => x.Status.Type == StatusType.Gratitude).Count(),
					IsInline = true
				});
                embed.AddField(new EmbedFieldBuilder() {
                    Name = "Выговоров:", Value = unit.UnitStatuses.Where(
                    x => x.Status.Type == StatusType.Reprimand || x.Status.Type == StatusType.SevereReprimand).Count(),
                    IsInline = true
                });
				embed.AddField(new EmbedFieldBuilder()
				{
					Name = "Активность за четыре недели:",
					Value = inLineUnitActivities
				});
				embed.AddField(new EmbedFieldBuilder() {
                    Name = "Всего активности:", Value = unit.Activities.Count(), IsInline = true
                });

                if (unit.Rank.Next != null)
                {
					embed.AddField(new EmbedFieldBuilder()
					{
						Name = "Счётчик на повышение",
						Value = unit.RankUpCounter.ToString() + "/" + unit.Rank.Next.CounterToReach.ToString(),
                        IsInline = true
					});
				}
				
                embed.WithFooter(new EmbedFooterBuilder().WithText((unit.SteamId == null ? "Steam ID не прикреплён. " : "")
                    + "Присоединился к клану: " + DateOnly.FromDateTime(unit.Joined).ToShortDateString()));
				embed.ThumbnailUrl = _guildProvider.GetGuild().GetUser(unit.DiscordId).GetAvatarUrl()
                    ?? _guildProvider.GetGuild().GetUser(unit.DiscordId).GetDefaultAvatarUrl();
                embed.WithColor(unit.Colour == null ? Color.DarkGreen : unit.GetProfileColor());

                await ModifyOriginalResponseAsync(func: (opt) =>
                {
                    opt.Embed = embed.Build();
                    opt.Content = "";
                });
            }
            else
            {
				await ModifyOriginalResponseAsync(func: (opt) => { opt.Content = "Пользователь не найден в системе"; });
			}
        }

		[SlashCommand("nickname", "Изменить никнейм пользователя")]
		public async Task ChangeNicknameCommand(string newNickname, IUser? targetUser = null)
		{
			try
			{
                if (targetUser == null)
                    targetUser = Context.User;

				var guild = _guildProvider.GetGuild();
				var guildUser = guild.GetUser(targetUser.Id);

				if (guildUser == null)
				{
					await RespondAsync("Пользователь не найден на сервере.", ephemeral: true);
					return;
				}
				
                if (Context.User != targetUser)
                {
					Unit? caller = await _db.Units.FindAsync(Context.User.Id);

                    if (caller == null)
                    {
                        await RespondAsync("Вы можете менять никнейм только себе", ephemeral: true);
                        return;
                    }

					bool canModerate = caller.HasPermission(PermissionType.ModerateNicknames);
					if (!canModerate)
					{
						await RespondAsync("Вы можете менять никнейм только себе", ephemeral: true);
						return;
					}
				}

				Unit? targetUnit = await _db.Units.FindAsync(targetUser.Id);
                string fullname;

                if (targetUnit != null)
                {
					if (targetUnit.Rank.Id > 1)
						fullname = "[РХБЗ] " + newNickname;
					else
						fullname = "[Р] " + newNickname;

					if (targetUnit != null)
					{
						targetUnit.Nickname = newNickname;
						await _db.SaveChangesAsync();
					}
				}
                else
                    fullname = newNickname;

                await guildUser.ModifyAsync(props => props.Nickname = fullname);

				await RespondAsync($"Никнейм пользователя '{targetUser.Username}' успешно изменён на '{newNickname}'");
			}
			catch (Exception ex)
			{
				await RespondAsync("Не удалось изменить никнейм.", ephemeral: true);
				_log.LogError(ex, $"Nickname change error: {ex.Message}");
			}


        }

		[IsUnit()]
		[SlashCommand("steam", "Привязать свой steam Id")]
        public async Task SteamIdCommand(string steamId)
        {
            try {
                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                ulong newId;
                if (unit == null)
                {
                    await RespondAsync("Ошибка: вы не найдены в системе.", ephemeral: true);

                    return;
                }
				if (!ulong.TryParse(steamId, out newId))
                {
                    await RespondAsync("Вы ввели некорректный Steam ID.", ephemeral: true);
                    return;
                }

                unit.SteamId = newId;
                await _db.SaveChangesAsync();
                await RespondAsync("Ваш Steam ID установлен на: " + unit.SteamId.ToString(), ephemeral: true);
			}
            catch(Exception ex) 
            {
				_log.LogError(ex, ex.StackTrace);
            }
        }

		[IsUnit()]
		[SlashCommand("color", "Изменить цвет профиля")]
        public async Task ChooseColorCommand()
        {
            var colors = new Dictionary<string, Color>
            {
                { "Зелёный", Color.Green },
                { "Красный", Color.Red },
                { "Синий", Color.Blue },
                { "Жёлтый", Color.Gold },
                { "Фиолетовый", Color.Purple },
                { "Бирюзовый", Color.Teal },
                { "Оранжевый", Color.Orange },
                { "Розовый", Color.Magenta },
                { "Белый", Color.LightGrey },
                { "Чёрный", Color.DarkerGrey }
            };

            var menu = new SelectMenuBuilder()
                .WithCustomId("profile-color-select")
                .WithPlaceholder("Выберите цвет профиля");

            foreach (var c in colors)
                menu.AddOption(c.Key, c.Value.RawValue.ToString());

            var builder = new ComponentBuilder()
                .WithSelectMenu(menu);

            await RespondAsync(
                text: "Выберите цвет, который будет использоваться в вашем профиле:",
                components: builder.Build(),
                ephemeral: true
            );
        }

		[IsUnit()]
		[ComponentInteraction("profile-color-select")]
        public async Task ColorsHandler(string[] selected)
        {
            try
            {
                string raw = selected.First();

                Unit? unit = await _db.Units.FindAsync(Context.User.Id);
                if (unit == null)
                {
                    await RespondAsync("Вы не найдены в системе.", ephemeral: true);
                    return;
                }

                uint rawValue = uint.Parse(raw);
                Color color = new Color(rawValue);

                unit.SetProfileColor(color);
                await _db.SaveChangesAsync();

                await RespondAsync(
                    $"Цвет профиля успешно изменён на `{color}`.",
                    ephemeral: true
                );
            }
            catch (Exception ex)
            {
                await RespondAsync("Не удалось изменить цвет профиля.", ephemeral: true);
				_log.LogError(ex, $"Colour select error: {ex.Message}");
            }
        }
    }
}
