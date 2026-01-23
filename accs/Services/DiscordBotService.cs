using accs.DiscordBot.Interactions;
using accs.Models;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.Services
{
    public class DiscordBotService : IDiscordBotService
    {
        private DiscordSocketClient _client;
		private ILogService _logger;

        public SocketGuild Guild { get; set; }

		public DiscordBotService(DiscordSocketClient client, ILogService logger)
        {
			_client = client;
			_logger = logger;
			//_client.UserVoiceStateUpdated += voiceChannelsModule.OnUserJoinedAsync;
			//_client.UserVoiceStateUpdated += voiceChannelsModule.OnUserLeftAsync;

            string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
            ulong guildId;
            if (ulong.TryParse(guildIdString, out guildId)) { throw _logger.ExceptionAsync("Cannot parse guild id!").Result; }

			Guild = _client.GetGuild(guildId);
			if (Guild == null) { throw _logger.ExceptionAsync("Guild is null!").Result; }
			if (!Guild.IsConnected) { throw _logger.ExceptionAsync("Not connected to guild!").Result; }
		}

        public async Task<bool> AddUserRoles(ulong userId, IEnumerable<ulong> roleIds)
        {
            SocketGuildUser user = Guild.GetUser(userId);
			await user.AddRolesAsync(roleIds);
			await _logger.WriteAsync("Added roles " + String.Join(", ", roleIds.Select(o => o.ToString()))
				+ " to " + (user?.Username).ToString()
				+ " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

        public async Task<bool> RemoveUserRoles(ulong userId, IEnumerable<ulong> roleIds)
        {
			SocketGuildUser user = Guild.GetUser(userId);
			await user.RemoveRolesAsync(roleIds);
			await _logger.WriteAsync("Removed roles " + String.Join(", ", roleIds.Select(o => o.ToString()))
				+ " from " + user.Username
				+ " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

		public async Task<bool> BanUser(ulong userId)
		{
			SocketGuildUser user = Guild.GetUser(userId);
			await user.BanAsync();
			await _logger.WriteAsync("Banned " + user.Username + " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

		public async Task<bool> KickUser(ulong userId)
		{
			SocketGuildUser user = Guild.GetUser(userId);
			await user.KickAsync();
			await _logger.WriteAsync("Kicked " + user.Username + " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}
	}
}
