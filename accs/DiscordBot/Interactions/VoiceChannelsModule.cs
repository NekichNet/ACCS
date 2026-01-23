using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using accs.Models;
using accs.DiscordBot.Interactions.Enums;

namespace accs.DiscordBot.Interactions
{
    [Group("voice", "Управление голосовыми каналами")]
    public class VoiceChannelsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IUnitRepository _unitRepository;
        private ILogService _logService;
        private DiscordSocketClient _discordSocketClient;
        private ulong _voiceChannelId;
        private SocketGuild? _guild;


        public VoiceChannelsModule(IActivityRepository activityRepository, IUnitRepository unitRepository, ILogService logService, DiscordSocketClient discordSocketClient) 
        {
            _unitRepository = unitRepository;
            _logService = logService;
            _discordSocketClient = discordSocketClient;

			string voiceChannelIdString = DotNetEnv.Env.GetString("VOICE_CHANNEL_ID", "null");
            if (!ulong.TryParse(voiceChannelIdString, out _voiceChannelId)) { throw _logService.ExceptionAsync("Cannot parse voice channel id!", LoggingLevel.Error).Result; }

			string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
			ulong guildId;
			if (ulong.TryParse(guildIdString, out guildId)) { throw _logService.ExceptionAsync("Cannot parse guild id!", LoggingLevel.Error).Result; }

            _guild = _discordSocketClient.GetGuild(guildId);
		}

        public async Task OnUserJoinedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
			if (after.VoiceChannel.Id == _voiceChannelId)
            {
                var channels = _guild.Channels;
                List<string> channelNames = new List<string>();

                foreach (var channel in channels) {
                    if (channel.ChannelType == Discord.ChannelType.Voice) 
                    {
                        if (channel.Name.Contains("【🔊】Пех"))
                        {
                            channelNames.Add(channel.Name);
                        }
                    }
                }

                int freeNumber = 1;
                while (channelNames.Contains($"【🔊】Пех {freeNumber}"))
                {
                    freeNumber++;
                }

                /// Channel creation and block for everyone to connect
                var newChannel = await _guild.CreateVoiceChannelAsync($"【🔊】Пех {freeNumber}", (props) => { props.Bitrate = 64000; props.UserLimit = 0; props.CategoryId = 0; });
                await newChannel.AddPermissionOverwriteAsync(_guild.EveryoneRole, new OverwritePermissions(connect: PermValue.Deny));

                /// Permission for needed users being granted
                var guildUser = _guild.GetUser(user.Id);
                await newChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(manageChannel:PermValue.Allow));
                await _guild.MoveAsync(guildUser, newChannel);
                Unit? unit = await _unitRepository.ReadAsync(user.Id);
                if (unit != null)
                {
                    foreach(var post in unit.Posts)
                    {
                        Post? postAbove = post.Head;
                        while (postAbove != null) 
                        {
                            if (postAbove.DiscordRoleId != null)
                            {
                                newChannel.AddPermissionOverwriteAsync(_guild.GetRole((ulong)postAbove.DiscordRoleId), new OverwritePermissions(connect: PermValue.Allow));
                            }
                        }
                    }
                }
            }
        }
        public async Task OnUserLeftAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (before.VoiceChannel.Users.Count == 0)
            {
                await before.VoiceChannel.DeleteAsync();
            }
        }

        [SlashCommand("access", "Передайте один из атрибутов: Опция \"clan\" открывает доступ на подключение всем участникам клана, опция \"friends\" открывает доступ всему клану, а также роли “Друг клана”.")] 
        public async Task OnGivingAccess(AccessChoices accessChoices)
        {
            if (accessChoices == AccessChoices.FRIEND)
            {
                await _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(_guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли френда
            }
            else if (accessChoices == AccessChoices.CLAN)
            {
                await _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(_guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли
            }
        }
        [SlashCommand("access-role", "Передайте один из атрибутов: Опция \"clan\" открывает доступ на подключение всем участникам клана, опция \"friends\" открывает доступ всему клану, а также роли “Друг клана”.")]
        public async Task OnGivingAccessByRole(SocketRole role)
        {
            _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(role, new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли
        }
    }
}
