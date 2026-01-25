using accs.DiscordBot.Preconditions;
using accs.Models.Enums;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [HasPermission(PermissionType.Administrator)]
    [Group("logs", "Команды для работы с логами")]
    public class LogsGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly LogService _logService;
        public LogsGroupModule(ILogService logService)
        {
            _logService = (LogService)logService;
        }

        [SlashCommand("get", "Получить список логов")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task GetLogs()
        {
            if (!Directory.Exists(_logService.LogDirectoryPath))
            {
                await RespondAsync("Папка логов не найдена.", ephemeral: true);
                return;
            }

            var files = Directory.GetFiles(_logService.LogDirectoryPath, "*.txt");

            if (files.Length == 0)
            {
                await RespondAsync("Логи отсутствуют.", ephemeral: true);
                return;
            }

            var menu = new SelectMenuBuilder()
                .WithCustomId("logs-select")
                .WithPlaceholder("Файлы логов")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                menu.AddOption(name, name);
            }

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await RespondAsync("Выберите файл лога:", components: builder.Build(), ephemeral: true);
        }

        [SlashCommand("cap", "Установить максимальное количество файлов логов")]
        public async Task SetCap(int count)
        {
            if (count < 0)
            {
                await RespondAsync("Число должно быть >= 0.", ephemeral: true);
                return;
            }
            _logService.MaxFilesCount = count;

            await RespondAsync($"Максимальное количество файлов логов установлено: {count}", ephemeral: true);
        }

        [SlashCommand("console", "Установить минимальный уровень логов для консоли")]
        public async Task SetConsoleLevel(LoggingLevel level)
        {
            _logService.ConsoleLogLevel = level;

            await RespondAsync($"Минимальный уровень логов для консоли установлен: {level}", ephemeral: true);
        }

        [SlashCommand("file", "Установить минимальный уровень логов для файлов")]
        public async Task SetFileLevel(LoggingLevel level)
        {
            _logService.FileLogLevel = level;

            await RespondAsync($"Минимальный уровень логов для файлов установлен: {level}", ephemeral: true);
        }

        [ComponentInteraction("logs-select")]
        public async Task LogsSelectHandler()
        {
            var component = (SocketMessageComponent)Context.Interaction;

            string fileName = component.Data.Values.First();

            string fullPath = Path.Join(_logService.LogDirectoryPath, fileName);

            if (!File.Exists(fullPath))
            {
                await RespondAsync("Файл не найден.", ephemeral: true);
                return;
            }

            await RespondAsync($"Файл {fileName}:", ephemeral: true);
            await FollowupWithFileAsync(fullPath, fileName, ephemeral: true);
        }
    }
}
