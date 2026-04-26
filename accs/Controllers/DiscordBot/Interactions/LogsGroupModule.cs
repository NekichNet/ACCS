using accs.DiscordBot.Preconditions;
using accs.Logging.Configurations;
using accs.Models.Enums;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;

namespace accs.Controllers.DiscordBot.Interactions
{
    [HasPermission(PermissionType.Administrator)]
    [Group("logs", "Команды для работы с логами")]
    public class LogsGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
		private FileLoggerConfiguration _currentConfig;

		public LogsGroupModule(IOptionsMonitor<FileLoggerConfiguration> config)
        {
			_currentConfig = config.CurrentValue;
		}

        [SlashCommand("get", "Получить список логов")]
        public async Task GetLogs()
        {
            if (!Directory.Exists(_currentConfig.DirectoryPath))
            {
                await RespondAsync("Папка логов не найдена.", ephemeral: true);
                return;
            }

            var files = Directory.GetFiles(_currentConfig.DirectoryPath, "*.log");

            if (!files.Any())
            {
                await RespondAsync("Логи отсутствуют.", ephemeral: true);
                return;
            }

            var menu = new SelectMenuBuilder()
                .WithCustomId("logs-select")
                .WithPlaceholder("Файлы логов")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var file in files.Take(23))
            {
                var name = Path.GetFileName(file);
                menu.AddOption(name, name);
            }

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await RespondAsync("Выберите файл лога:", components: builder.Build(), ephemeral: true);
        }

        [ComponentInteraction("logs-select", ignoreGroupNames: true)]
        public async Task LogsSelectHandler(string[] selectedIds)
        {
            string fileName = selectedIds.First();
            string fullPath = Path.Join(_currentConfig.DirectoryPath, fileName);

            if (!File.Exists(fullPath))
            {
                await RespondAsync("Файл не найден.", ephemeral: true);
                return;
            }

            await RespondWithFileAsync(fullPath, fileName, ephemeral: true);
        }
    }
}
