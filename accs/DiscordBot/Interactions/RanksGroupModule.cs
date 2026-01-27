using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
	[Group("rank", "Команды для управления званиями")]
	public class RanksGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;
		private readonly IGuildProviderService _guildProvider;


		public RanksGroupModule(AppDbContext db, IGuildProviderService guildProvider, ILogService logService)
        {
			_db = db;
			_guildProvider = guildProvider;
            _logService = logService;
        }

		[HasPermission(PermissionType.ChangeRanks)]
		[SlashCommand("up", "Повысить бойца на одно звание")]
        public async Task RankUpCommandAsync(IUser targetedUser)
        {
			try
			{
                var targetUnit = await _db.Units.Include(u => u.Rank)
					.FirstOrDefaultAsync(u => u.DiscordId == targetedUser.Id);

				if (targetUnit == null)
				{
					await RespondAsync("Боец не найден.", ephemeral: true);
					return;
				}

				Rank? rank = targetUnit.Rank.Next;
				if (rank == null)
				{
					await RespondAsync($"У бойца {targetUnit.Nickname} уже самое высокое на данный момент звание: {targetUnit.Rank.Name}.", ephemeral: true);
					await _logService.WriteAsync($"У бойца {targetUnit.Nickname} уже самое высокое на данный момент звание: {targetUnit.Rank.Name}.", LoggingLevel.Debug);
					return;
				}

				List<IRole> roles = new List<IRole>();

				if (rank.DiscordRoleId != null)
					roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)rank.DiscordRoleId));

				await _guildProvider.GetGuild().GetUser(targetedUser.Id).AddRolesAsync(roles);

				if (targetUnit.Rank.DiscordRoleId != null)
					await _guildProvider.GetGuild().GetUser(targetedUser.Id).RemoveRoleAsync((ulong)targetUnit.Rank.DiscordRoleId);

				targetUnit.Rank = rank;
				targetUnit.RankUpCounter = 0;

				await _db.SaveChangesAsync();

                await RespondAsync($"Боец {targetUnit.Nickname} повышен до звания {targetUnit.Rank.Name}. Счётчик на повышение сброшен.");
			}
			catch (Exception ex)
			{
				await _logService.WriteAsync(ex.Message, LoggingLevel.Error);
			}
		}

		[HasPermission(PermissionType.ChangeRanks)]
		[SlashCommand("set", "Установить бойцу выбранное звание")]
		public async Task SetRankCommandAsync(IUser targetedUser, int? rankId = null)
		{
			try
			{
                var targetUnit = await _db.Units.FindAsync(targetedUser.Id);

				if (targetUnit == null)
				{
					await RespondAsync("Боец не найден.", ephemeral: true);
					return;
				}

				if (rankId == null)
				{
					/// На чёрный день
					/*
					var allowedRanks = new List<Rank>(); 
					Rank CurrentTestRank = actorUnit.Rank;
					while (CurrentTestRank.Previous != null)
					{
						CurrentTestRank = CurrentTestRank.Previous;
						allowedRanks.Add(CurrentTestRank);
					}
					*/

					var allowedRanks = await _db.Ranks.ToListAsync();



                    if (!allowedRanks.Any())
					{
						await RespondAsync("Нет доступных должностей для назначения.", ephemeral: true);
						return;
					}

					var menu = new SelectMenuBuilder()
						.WithCustomId($"rank-menu-{targetedUser.Id}")
						.WithPlaceholder("Звание")
						.WithMinValues(1)
						.WithMaxValues(1);

					foreach (var rank in allowedRanks)
						menu.AddOption(rank.Name, rank.Id.ToString());

					var builder = new ComponentBuilder()
						.WithSelectMenu(menu);

					await RespondAsync(
						$"Выберите новое звание для {targetUnit.Nickname}",
						components: builder.Build(),
						ephemeral: true);
				}
				else
				{
					Rank? rank = await _db.Ranks.FindAsync(rankId.Value);
                    if (rank == null)
					{
						await RespondAsync($"Звание c Id {rankId} не найдено.", ephemeral: true);
						await _logService.WriteAsync($"Звание c Id {rankId} не найдено.", LoggingLevel.Error);
						return;
					}
					if (targetUnit.Rank.DiscordRoleId != null)
						await _guildProvider.GetGuild().GetUser(targetedUser.Id).RemoveRoleAsync((ulong)targetUnit.Rank.DiscordRoleId);

					targetUnit.Rank = rank;
					targetUnit.RankUpCounter = 0;

					List<IRole> roles = new List<IRole>();

					if (rank.DiscordRoleId != null)
						roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)rank.DiscordRoleId));

					await _guildProvider.GetGuild().GetUser(targetedUser.Id).AddRolesAsync(roles);

					await _db.SaveChangesAsync();

                    await RespondAsync($"Установлено звание {rank.Name} для бойца {targetUnit.Nickname}. Счётчик на повышение сброшен.");
				}
			}
			catch (Exception ex)
			{
				await _logService.WriteAsync(ex.Message, LoggingLevel.Error);
			}
		}

		[HasPermission(PermissionType.ChangeRanks)]
		[ComponentInteraction("rank-menu-*", ignoreGroupNames: true)]
        public async Task RankMenuHandler(string targetIdString, string[] selectedValues)
        {
			ulong targetId = ulong.Parse(targetIdString);
			try
            {
                Unit? targetUnit = await _db.Units.FindAsync(targetId);

                string selectedRankIdRaw = selectedValues.First();
				int selectedRankId = int.Parse(selectedRankIdRaw);
                Rank? rank = await _db.Ranks.FindAsync(selectedRankId);

                if (targetUnit == null)
				{
					await RespondAsync($"Боец с Id {targetId} не найден в системе.", ephemeral: true);
					await _logService.WriteAsync($"Боец с Id {targetId} не найден в системе.", LoggingLevel.Error);
					return;
				}

				if (rank == null)
                {
					await RespondAsync($"Звание c Id {selectedRankIdRaw} не найдено.", ephemeral: true);
                    await _logService.WriteAsync($"Звание c Id {selectedRankIdRaw} не найдено.", LoggingLevel.Error);
                    return;
                }

				if (targetUnit.Rank.DiscordRoleId != null)
					await _guildProvider.GetGuild().GetUser(targetId).RemoveRoleAsync((ulong)targetUnit.Rank.DiscordRoleId);

				// Присвоение звания
				targetUnit.Rank = rank;
                targetUnit.RankUpCounter = 0;

				if (rank.DiscordRoleId != null)
					await _guildProvider.GetGuild().GetUser(targetId).AddRoleAsync((ulong)rank.DiscordRoleId);

				await _db.SaveChangesAsync();

				await RespondAsync($"Установлено звание {rank.Name} для бойца {targetUnit.Nickname}. Счётчик на повышение сброшен.");
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка в RankMenuHandler: {ex.Message}", LoggingLevel.Error);
				await RespondAsync("Ошибка при обновлении должностей.", ephemeral: true);
            }
        }
    }
}
