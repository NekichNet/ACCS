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
    public class VoiceChannellsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IActivityRepository _activityRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;
        private DiscordSocketClient _discordSocketClient;
        private SocketGuild? _guild { get { return _discordSocketClient.GetGuild(0); } } // Подставь айдишник сервера


        public VoiceChannellsModule(IActivityRepository activityRepository, IUnitRepository unitRepository, ILogService logService, DiscordSocketClient discordSocketClient) 
        {
            _activityRepository = activityRepository;
            _unitRepository = unitRepository;
            _logService = logService;
            _discordSocketClient = discordSocketClient;
        }
        
        public async Task OnUserJoinedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
                if (after.VoiceChannel.Id == 0) // Поставить айдишник того канала для создания новых голосовых чатов
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
                _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(_guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли френда
            }
            else if (accessChoices == AccessChoices.CLAN)
            {
                _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(_guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли
            }
        }
        [SlashCommand("access-role", "Передайте один из атрибутов: Опция \"clan\" открывает доступ на подключение всем участникам клана, опция \"friends\" открывает доступ всему клану, а также роли “Друг клана”.")]
        public async Task OnGivingAccessByRole(SocketRole role)
        {
            _guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(role, new OverwritePermissions(connect: PermValue.Allow)); //подставь Id роли
        }
    }
}
