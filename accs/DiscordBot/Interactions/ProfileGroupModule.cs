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
    [IsUnit()]
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
                embed.WithFooter(new EmbedFooterBuilder().WithText("Присоединился к клану: " + unit.Joined));
				embed.ThumbnailUrl = _guildProvider.GetGuild().GetUser(unit.DiscordId).GetAvatarUrl()
                    ?? _guildProvider.GetGuild().GetUser(unit.DiscordId).GetDefaultAvatarUrl();
                embed.WithColor(unit.Color == null ? Color.DarkGreen : unit.GetColor());

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

				if (targetUnit.Rank.Id > 1)
                    fullname = "[РХБЗ] " + newNickname;
                else
					fullname = "[Р] " + newNickname;

				await guildUser.ModifyAsync(props => props.Nickname = fullname);
				
				if (targetUnit != null)
				{
					targetUnit.Nickname = newNickname;
					await _db.SaveChangesAsync();
				}

				await RespondAsync($"Никнейм пользователя '{targetUser.Username}' успешно изменён на '{newNickname}'");
			}
			catch (Exception ex)
			{
				await RespondAsync("Не удалось изменить никнейм.", ephemeral: true);
				await _logService.WriteAsync($"Nickname change error: {ex.Message}", LoggingLevel.Error);
			}


        }

        [SlashCommand("steam", "Привязать свой steam Id")]
        public async Task SteamIdCommand(ulong steamId)
        {
            try {
                var user = Context.Interaction.User;
                (await _db.Units.FindAsync(user.Id)).SteamId = steamId;
            }
            catch(Exception ex) 
            {
                await _logService.WriteAsync(ex.StackTrace);
            }
        }
    }
}
