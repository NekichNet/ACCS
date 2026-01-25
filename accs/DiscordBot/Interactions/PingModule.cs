using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    public class PingModule : InteractionModuleBase<SocketInteractionContext>
	{
        [SlashCommand("ping", "Проверить, работает ли бот")]
        public async Task Ping()
        {
            await RespondAsync("Ok");
        }
    }
}
