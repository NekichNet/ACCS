using accs.Database;
using accs.DiscordBot.Interactions.Enums;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace accs.DiscordBot.Interactions
{
    [Group("voice", "Управление голосовыми каналами")]
    public class VoiceChannelsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogService _logService;
		private readonly DiscordSocketClient _client;
        private readonly IGuildProviderService _guildProvider;
        private readonly AppDbContext _db;

		private ulong _voiceChannelId;

        public VoiceChannelsModule(ILogService logService, DiscordSocketClient discordSocketClient, IGuildProviderService guildProvider, AppDbContext db) 
        {
            _logService = logService;
            _client = discordSocketClient;
            _guildProvider = guildProvider;
            _db = db;
		}

        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);

			string voiceChannelIdString = DotNetEnv.Env.GetString("VOICE_CHANNEL_ID", "null");
			if (!ulong.TryParse(voiceChannelIdString, out _voiceChannelId)) { _logService.WriteAsync("Cannot parse voice channel id!", LoggingLevel.Error); }

			_client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
		}

        public async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
			SocketGuild guild = _guildProvider.GetGuild();

			if (before.VoiceChannel is SocketVoiceChannel)
            {
				if (before.VoiceChannel.ConnectedUsers.Count == 0 && before.VoiceChannel.Id != _voiceChannelId && (before.VoiceChannel.Name.StartsWith("【🔊】") || before.VoiceChannel.Name.StartsWith("【🎧】")))
				{
					await before.VoiceChannel.DeleteAsync();
				}
			}

            if (after.VoiceChannel == null)
                return;

			if (after.VoiceChannel.Id == _voiceChannelId)
            {
                var channels = guild.Channels;
                List<string> channelNames = new List<string>();

                foreach (var channel in channels) {
                    if (channel.ChannelType == ChannelType.Voice) 
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

                ulong voiceCategoryId;
                if (!ulong.TryParse(DotNetEnv.Env.GetString("VOICE_CATEGORY_ID", "VOICE_CATEGORY_ID not found"), out voiceCategoryId))
                {
                    await _logService.WriteAsync("Cannot parse voice category id!", LoggingLevel.Error);
                    return;
                }

                /// Channel creation and block for everyone to connect
                var newChannel = await guild.CreateVoiceChannelAsync($"【🔊】Пех {freeNumber}", (props) => { props.Bitrate = 64000; props.UserLimit = null; props.CategoryId = voiceCategoryId; });

                /// Permission for needed users being granted
                SocketGuildUser guildUser = guild.GetUser(user.Id);
                await newChannel.AddPermissionOverwriteAsync(guildUser, new OverwritePermissions(manageChannel: PermValue.Allow));
                await guild.MoveAsync(guildUser, newChannel);
            }
        }

        [SlashCommand("access", "Откройте доступ к каналу для клана или для всех")] 
        public async Task OnGivingAccess(AccessChoices accessChoices)
        {
			SocketGuild guild = _guildProvider.GetGuild();

			if (accessChoices == AccessChoices.Friend)
            {
                await guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow));
            }
            else if (accessChoices == AccessChoices.Clan)
            {
                await guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(guild.GetRole(0), new OverwritePermissions(connect: PermValue.Allow));
            }
        }


        [SlashCommand("access-role", "Откройте доступ к каналу для определённой роли")]
        public async Task OnGivingAccessByRole(SocketRole role)
        {
			SocketGuild guild = _guildProvider.GetGuild();
			await guild.GetChannel(Context.Interaction.Channel.Id).AddPermissionOverwriteAsync(role, new OverwritePermissions(connect: PermValue.Allow));
        }
    }
}
