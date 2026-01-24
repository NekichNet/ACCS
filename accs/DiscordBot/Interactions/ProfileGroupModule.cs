using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    public class ProfileGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogService _logService;
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly SocketGuild _guild;
        
        public ProfileGroupModule(IUnitRepository unitRepository, ILogService logservice, DiscordSocketClient discordSocketClient) 
        {
            _unitRepository = unitRepository;
            _logService = logservice;
            _discordSocketClient = discordSocketClient;
            string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
            ulong guildId;
            if (!ulong.TryParse(guildIdString, out guildId)) { throw _logService.ExceptionAsync("Cannot parse guild id!", LoggingLevel.Error).Result; }

            _guild = _discordSocketClient.GetGuild(guildId);
        }

        [SlashCommand("profile", "Показать профиль указанного пользователя")]
        public async Task ShowUserProfile(IUser? user = null)
        {
            Unit? unit;
            if (user == null) { unit = await _unitRepository.ReadAsync(Context.User.Id); }
            else { unit = await _unitRepository.ReadAsync(user.Id); }

            if (unit != null)
            {
				Console.WriteLine(Context.User.Username);
				Console.WriteLine(unit.Nickname);
                Console.WriteLine(unit.Rank);
				Console.WriteLine(unit.DiscordId);
				Console.WriteLine(unit.SteamId);
				Console.WriteLine(unit.Posts.Count);

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
                    Title = $"{unit.Rank} {unit.Nickname}",
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
                

                embed.AddField(new EmbedFieldBuilder() { Name = "Статусы:", IsInline = false, Value = inLineUnitStatuses });
                embed.AddField(new EmbedFieldBuilder() { Name = "Награды:", IsInline = false, Value = inLineUnitRewards });
                embed.AddField(new EmbedFieldBuilder() { Name = "Благодарности:", IsInline = true, Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Gratitude).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Выговоров:", IsInline = true, Value = unit.UnitStatuses.Where(x => x.Status.Type == StatusType.Reprimand || x.Status.Type == StatusType.SevereReprimand).Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Дней активности:", IsInline = true, Value = unit.Activities.Count() });
                embed.AddField(new EmbedFieldBuilder() { Name = "Активность в последние 14 дней:", IsInline = true, Value = inLineUnitActivities });
                embed.ThumbnailUrl = _guild.GetUser(unit.DiscordId).GetAvatarUrl();
            }
            else
            {
                await DeleteOriginalResponseAsync();
                await RespondAsync($"Пользователь не найден в системе", ephemeral: true);
            }
        }
    }
}
