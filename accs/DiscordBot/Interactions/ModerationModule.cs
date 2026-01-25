using accs.DiscordBot.Preconditions;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Polly;
using System.Drawing;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [Group("moderation", "Команды модерации: кик и бан")]
    public class ModerationModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogService _logService;

        public ModerationModule(ILogService logService)
        {
            _logService = logService;
        }

        [SlashCommand("kick", "Кикнуть участника с сервера")]
        public async Task KickUserCommand(IUser target, string? reason = null)
        {
            try
            {
                var moderator = Context.User as SocketGuildUser; 
                var targetUser = target as SocketGuildUser;

                if (!moderator.GuildPermissions.KickMembers)
                {
                    await RespondAsync("У вас нет разрешения на кик участников.", ephemeral: true);
                    return;
                }

                if (targetUser == null)
                {
                    await RespondAsync("Пользователь не найден на сервере.", ephemeral: true);
                    return;
                }

                await targetUser.KickAsync(reason ?? "Kick command issued");

                await RespondAsync($"Пользователь '{target.Username}' был кикнут.\nПричина: {reason ?? "не указана"}");

                await _logService.WriteAsync(
                    $"Moderator {moderator.Username} kicked {target.Username}. Reason: {reason}",
                    LoggingLevel.Info);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"KickUserAsync error: {ex.Message}", LoggingLevel.Error); await RespondAsync("Ошибка при попытке кикнуть пользователя.", ephemeral: true);
            }
        }


        [SlashCommand("ban", "Забанить участника на сервере")]
        public async Task BanUserCommand(IUser target, string? reason = null)
        {
            try
            {
                var moderator = Context.User as SocketGuildUser;

                if (!moderator.GuildPermissions.BanMembers)
                {
                    await RespondAsync("У вас нет разрешения на бан участников.", ephemeral: true);
                    return;
                }

                await Context.Guild.AddBanAsync(target.Id, reason: reason ?? "Ban command issued");

                await RespondAsync($"Пользователь '{target.Username}' был забанен.\nПричина: {reason ?? "не указана"}");

                await _logService.WriteAsync(
                    $"Moderator {moderator.Username} banned {target.Username}. Reason: {reason}",
                    LoggingLevel.Info);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"BanUserAsync error: {ex.Message}", LoggingLevel.Error); 
                await RespondAsync("Ошибка при попытке забанить пользователя.", ephemeral: true);
            }
        }
    }
}
