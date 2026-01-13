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
		private InteractionService _interaction;
		private ILogService _logger;

        private SocketGuild _guild;

		public DiscordBotService(DiscordSocketClient client, ILogService logger)
        {
			_client = client;
			_interaction = new InteractionService(_client.Rest);
			_logger = logger;
			_client.Log += async (msg) =>
			{
				await Task.CompletedTask;
				Console.WriteLine(msg);
			};

            string token = DotNetEnv.Env.GetString("TOKEN", "Token not found");
            if (token == "Token not found") { throw _logger.ExceptionAsync(token).Result; }

            string guildIdString = DotNetEnv.Env.GetString("SERVER_ID", "Server id not found");
            ulong guildId;
            if (ulong.TryParse(guildIdString, out guildId)) { throw _logger.ExceptionAsync("Cannot parse guild id!").Result; }

			_client.LoginAsync(TokenType.Bot, token).Wait();
            if (_client.LoginState != LoginState.LoggedIn) { throw _logger.ExceptionAsync("Cannot log in!").Result; }
			_client.StartAsync();

			_guild = _client.GetGuild(guildId);
			if (_guild == null) { throw _logger.ExceptionAsync("Guild is null!").Result; }
			if (!_guild.IsConnected) { throw _logger.ExceptionAsync("Not connected to guild!").Result; }
		}

        public async Task<bool> AddUserRoles(ulong userId, IEnumerable<ulong> roleIds)
        {
            SocketGuildUser user = _guild.GetUser(userId);
			await user.AddRolesAsync(roleIds);
			_logger.WriteAsync("Added roles " + String.Join(", ", roleIds.Select(o => o.ToString()))
				+ " to " + (user?.Username).ToString()
				+ " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

        public async Task<bool> RemoveUserRoles(ulong userId, IEnumerable<ulong> roleIds)
        {
			SocketGuildUser user = _guild.GetUser(userId);
			await user.RemoveRolesAsync(roleIds);
			_logger.WriteAsync("Removed roles " + String.Join(", ", roleIds.Select(o => o.ToString()))
				+ " from " + user.Username
				+ " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

		public async Task<bool> BanUser(ulong userId)
		{
			SocketGuildUser user = _guild.GetUser(userId);
			await user.BanAsync();
			_logger.WriteAsync("Banned " + user.Username + " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}

		public async Task<bool> KickUser(ulong userId)
		{
			SocketGuildUser user = _guild.GetUser(userId);
			await user.KickAsync();
			_logger.WriteAsync("Kicked " + user.Username + " with id " + userId,
				user == null ? LoggingLevel.Error : LoggingLevel.Debug);
			return user != null;
		}
	}
}
