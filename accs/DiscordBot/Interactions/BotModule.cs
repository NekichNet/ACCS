using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    public class BotModule : InteractionModuleBase<SocketInteractionContext>
	{
        [SlashCommand("ping", "Проверить, работает ли бот")]
        public async Task PingCommand()
        {
            await RespondAsync("Ok");
        }

        [SlashCommand("help", "Вывести список всех доступных Вам команд")]
        public async Task HelpCommand()
        {

        }
    }
}
