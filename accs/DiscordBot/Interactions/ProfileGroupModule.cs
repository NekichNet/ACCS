using accs.Database;
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
    public class ProfileGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;
        private readonly IGuildProviderService _guildProvider;
        
        public ProfileGroupModule(AppDbContext db, ILogService logservice, IGuildProviderService guildProvider) 
        {
            _db = db;
            _logService = logservice;
            _guildProvider = guildProvider;
        }

		[IsUnit()]
		[SlashCommand("profile", "Показать профиль указанного пользователя")]
        public async Task ShowProfileCommand(IUser? user = null)
        {
            Unit? unit;
            if (user == null) { unit = await _db.Units.FindAsync(Context.User.Id); }
            else { unit = await _db.Units.FindAsync(user.Id); }

            if (unit != null)
            {
				string embedDescription = string.Empty;

                if (unit.Posts.Count > 1)
                {
                    foreach (var post in unit.Posts)
                    {
                        embedDescription += $"{post.Name}";
                        if (post != unit.Posts.Last())
                        {
                            embedDescription += ", ";
                        }
                    }
                }
                else
                {
                    embedDescription += $"{unit.Posts[0].Name}";
                }

                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"{unit.Rank.Name} {unit.GetOnlyNickname()}",
                    Description = embedDescription
                };

                var unitStatuses = unit.UnitStatuses.Where(x=>x.EndDate > DateTime.UtcNow || x.EndDate == null);
                string inLineUnitStatuses = string.Empty;
                foreach (var unitStatus in unitStatuses)
                {
                    inLineUnitStatuses += $"{unitStatus.Status.Name}";
                    if (unitStatus != unitStatuses.Last())
                    {
                        inLineUnitStatuses += ", ";
                    }
                }


                var unitRewards = unit.Rewards;
                string inLineUnitRewards = string.Empty;
                foreach (var unitReward in unitRewards)
                {
                    inLineUnitRewards += $"{unitReward.Name}";
                    if (unitReward != unitRewards.Last())
                    {
                        inLineUnitRewards += ", ";
                    }
                }

                string inLineUnitActivities = string.Empty;
                for (int i = -13; i <= 0; i++)
                {
                    if(unit.Activities.Any(a => a.Date == DateOnly.FromDateTime(DateTime.Today).AddDays(i)))
                    {
                        inLineUnitActivities += ":green_square:";
                    }
                    else
                    {
                        inLineUnitActivities += ":black_medium_square:";
                    }
                    if (i == -7)
                        inLineUnitActivities += '\n';
                }
                
                if (inLineUnitStatuses.Length > 0)
                    embed.AddField(new EmbedFieldBuilder() { Name = "Статусы:", Value = inLineUnitStatuses });
                if (inLineUnitRewards.Length > 0)
                    embed.AddField(new EmbedFieldBuilder() { Name = "Награды:", Value = inLineUnitRewards });
                embed.AddField(new EmbedFieldBuilder() { Name = "Благодарности:", Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Gratitude).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Выговоров:", Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Reprimand || x.Status.Type == StatusType.SevereReprimand).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Дней активности:", Value = unit.Activities.Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Из них последние 14:", Value = inLineUnitActivities });
                embed.WithFooter(new EmbedFooterBuilder().WithText("Присоединился к клану: " + DateOnly.FromDateTime(unit.Joined).ToShortDateString()));
				embed.ThumbnailUrl = _guildProvider.GetGuild().GetUser(unit.DiscordId).GetAvatarUrl()
                    ?? _guildProvider.GetGuild().GetUser(unit.DiscordId).GetDefaultAvatarUrl();
                embed.WithColor(unit.Colour == null ? Color.DarkGreen : unit.GetProfileColor());

                await RespondAsync(embed: embed.Build());
            }
            else
            {
                await DeleteOriginalResponseAsync();
                await RespondAsync($"Пользователь не найден в системе", ephemeral: true);
            }
        }

		[SlashCommand("nickname", "Изменить никнейм пользователя")]
		public async Task ChangeNicknameCommand(string newNickname, SocketUser? targetUser = null)
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
				await _logService.WriteAsync($"Nickname change error: {ex.Message}", LoggingLevel.Error);
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
                await _logService.WriteAsync(ex.StackTrace);
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
                await _logService.WriteAsync($"Colour select error: {ex.Message}", LoggingLevel.Error);
            }
        }
    }
}
