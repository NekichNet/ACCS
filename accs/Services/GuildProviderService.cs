using accs.Services.Interfaces;
using Discord.WebSocket;
using DotNetEnv;

namespace accs.Services
{
    public class GuildProviderService : IGuildProviderService
    {
        private DiscordSocketClient _client;

		private SocketGuild? _guild;
        private ulong _guildId;

        public GuildProviderService(DiscordSocketClient client)
        {
            _client = client;
			if (!ulong.TryParse(Env.GetString("GUILD_ID", "Guild id not found"), out _guildId))
				throw new NullReferenceException("Guild id is null!");
		}

        public SocketGuild GetGuild()
        {
            if (_guild == null)
                InitGuild();
            return _guild;
		}

		public ulong GetGuildId()
        {
            return _guildId;
        }

        private void InitGuild()
        {
            _guild = _client.GetGuild(_guildId);
            if (_guild == null)
                throw new NullReferenceException("Guild is null!");
		}
    }
}
