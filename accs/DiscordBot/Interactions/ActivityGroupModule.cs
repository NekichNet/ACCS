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
    [Group("fix", "Зафиксировать активность")]
    public class ActivityGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IActivityRepository _activityRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;
        private bool _autoVerify = false;

        public ActivityGroupModule(IActivityRepository activityRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _activityRepository = activityRepository;
            _unitRepository = unitRepository;
            _logService = logService;
        }

        [SlashCommand("voice", "всех бойцов в голосовом канале")]
        public async Task FixVoiceCommand([ChannelTypes(ChannelType.Voice, ChannelType.Forum)] IChannel channel)
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
                // Сделать Select Menu с выбором игроков
				ComponentBuilder builder = new ComponentBuilder();
				builder.WithButton("Верифицировать", $"verify_{today}:" + String.Join(",", ids));

                await ReplyAsync("", components: builder.Build());
			}
            
        }
    }
}
