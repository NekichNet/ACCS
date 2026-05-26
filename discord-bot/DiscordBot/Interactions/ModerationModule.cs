using accs.Database;
using accs.Controllers.DiscordBot.Preconditions;
using accs.Models.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace discord_bot.DiscordBot.Interactions
{
    [IsUnit()]
    public class ModerationModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger<ModerationModule> _log;
        private readonly AppDbContext _db;

        public ModerationModule(ILogger<ModerationModule> log, AppDbContext db)
        {
            _log = log;
            _db = db;
        }

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [SlashCommand("dismiss-user", "Уволить бойца.")]
        public async Task DismissUnitCommand(IUser target)
        {
            Unit? unit = await _db.Units.FindAsync(target.Id);

            if (unit != null)
            {
				SocketGuildUser user = Context.Guild.GetUser(target.Id);
				if (user != null)
				{
					foreach (Post post in unit.Posts)
					{
						List<IRole> roles = new List<IRole>();
						if (post.DiscordRoleId != null)
							roles.Add(await Context.Guild.GetRoleAsync((ulong)post.DiscordRoleId));
						Subdivision? subdiv = post.Subdivision;
						while (subdiv != null)
						{
							if (subdiv.DiscordRoleId != null)
								roles.Add(await Context.Guild.GetRoleAsync((ulong)subdiv.DiscordRoleId));
							subdiv = subdiv.Head;
						}

						await user.RemoveRolesAsync(roles);
					}

					unit.Posts.Clear();
					if (unit.Rank.DiscordRoleId != null)
						await user.RemoveRoleAsync((ulong)unit.Rank.DiscordRoleId);
					await RespondAsync($"{unit.GetOnlyNickname()} был уволен.");
				}
				else
				{
					unit.Posts.Clear();
					await RespondAsync($"{unit.GetOnlyNickname()} был уволен, но не удалось снять роли.");
				}

                await _db.SaveChangesAsync();
			}
            else
            {
                await RespondAsync($"Пользователь {target.GlobalName} не найден в системе.", ephemeral: true);
            }
        }

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
		[SlashCommand("dismiss-id", "Уволить бойца по Discord ID.")]
		public async Task DismissUnitCommand(string id)
		{
            ulong userId;
            if (!ulong.TryParse(id, out userId))
            {
                await RespondAsync("Неверный Discord ID бойца.");
            }

			Unit? unit = await _db.Units.FindAsync(userId);

			if (unit != null)
			{
				SocketGuildUser user = Context.Guild.GetUser(userId);
                if (user != null)
                {
					foreach (Post post in unit.Posts)
					{
						List<IRole> roles = new List<IRole>();
						if (post.DiscordRoleId != null)
							roles.Add(await Context.Guild.GetRoleAsync((ulong)post.DiscordRoleId));
						Subdivision? subdiv = post.Subdivision;
						while (subdiv != null)
						{
							if (subdiv.DiscordRoleId != null)
								roles.Add(await Context.Guild.GetRoleAsync((ulong)subdiv.DiscordRoleId));
							subdiv = subdiv.Head;
						}

						await user.RemoveRolesAsync(roles);
					}

					unit.Posts.Clear();
					if (unit.Rank.DiscordRoleId != null)
						await user.RemoveRoleAsync((ulong)unit.Rank.DiscordRoleId);
					await RespondAsync($"{unit.GetOnlyNickname()} был уволен.");
				}
                else
                {
                    unit.Posts.Clear();
					await RespondAsync($"{unit.GetOnlyNickname()} был уволен, но не удалось снять роли.");
				}

				await _db.SaveChangesAsync();
			}
			else
			{
				await RespondAsync($"Пользователь c ID {userId} не найден в системе.", ephemeral: true);
			}
		}

		[DefaultMemberPermissions(GuildPermission.KickMembers)]
        [SlashCommand("kick", "Выгнать участника с сервера.")]
        public async Task KickUserCommand(IUser target, string? reason = null)
        {
            try
            {
                var moderator = Context.User as SocketGuildUser; 
                var targetUser = target as SocketGuildUser;

                if (targetUser == null)
                {
                    await RespondAsync("Пользователь не найден на сервере.", ephemeral: true);
                    return;
                }

                Unit? targetUnit = await _db.Units.FindAsync(targetUser.Id);
                if (targetUnit != null)
                {
                    targetUnit.Posts.Clear();
                }

                await targetUser.KickAsync(reason ?? "Kick command issued");

                await RespondAsync($"Пользователь '{target.Username}' был кикнут.\nПричина: {reason ?? "не указана"}");

				_log.LogInformation($"Moderator {moderator.Username} kicked {target.Username}. Reason: {reason}");
            }
            catch (Exception ex)
            {
				_log.LogError(ex, $"KickUserAsync error: {ex.Message}");
                await RespondAsync("Ошибка при попытке кикнуть пользователя.", ephemeral: true);
            }
        }

        [DefaultMemberPermissions(GuildPermission.BanMembers)]
        [SlashCommand("ban", "Забанить участника на сервере")]
        public async Task BanUserCommand(IUser target, string? reason = null)
        {
            try
            {
                var moderator = Context.User as SocketGuildUser;

                await Context.Guild.AddBanAsync(target.Id, reason: reason);

                await RespondAsync($"Пользователь '{target.Username}' был забанен.\nПричина: {reason ?? "не указана"}");

				_log.LogInformation($"Moderator {moderator.Username} banned {target.Username}. Reason: {reason}");
            }
            catch (Exception ex)
            {
				_log.LogError($"BanUserAsync error: {ex.Message}"); 
                await RespondAsync("Ошибка при попытке забанить пользователя.", ephemeral: true);
            }
        }
    }
}
