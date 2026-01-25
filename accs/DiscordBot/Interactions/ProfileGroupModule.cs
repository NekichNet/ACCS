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
        public async Task ShowUserProfile(IUser? user = null)
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
                    Title = $"{unit.Rank.Name} {unit.Nickname}",
                    Description = embedDescription
                };

                var unitStatuses = unit.UnitStatuses.Where(x=>x.EndDate < DateTime.Today);
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

                var unitActivities = unit.Activities;
                string inLineUnitActivities = string.Empty;

                for (int i = 0; i >= -14; i--)
                {
                    if(unitActivities.Contains(new Activity { Unit = unit, Date = DateOnly.FromDateTime(DateTime.Today).AddDays(i) }))
                    {
                        inLineUnitActivities += "🟩";
                    }
                    else
                    {
                        inLineUnitActivities += "⬜";
                    }
                }
                
                if (inLineUnitStatuses.Length > 0)
                    embed.AddField(new EmbedFieldBuilder() { Name = "Статусы:", Value = inLineUnitStatuses });
                if (inLineUnitRewards.Length > 0)
                    embed.AddField(new EmbedFieldBuilder() { Name = "Награды:", Value = inLineUnitRewards });
                embed.AddField(new EmbedFieldBuilder() { Name = "Благодарности:", Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Gratitude).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Выговоров:", Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Reprimand || x.Status.Type == StatusType.SevereReprimand).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Дней активности:", Value = unit.Activities.Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Активность в последние 14 дней:", Value = inLineUnitActivities });
				embed.ThumbnailUrl = _guildProvider.GetGuild().GetUser(unit.DiscordId).GetAvatarUrl()
                    ?? _guildProvider.GetGuild().GetUser(unit.DiscordId).GetDefaultAvatarUrl();
                embed.WithColor(Color.DarkGreen);

                await RespondAsync(embed: embed.Build());
            }
            else
            {
                await DeleteOriginalResponseAsync();
                await RespondAsync($"Пользователь не найден в системе", ephemeral: true);
            }
        }
    }
}
