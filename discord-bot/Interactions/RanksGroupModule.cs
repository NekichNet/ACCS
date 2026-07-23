using Business.Database;
using discord_bot.Preconditions;
using Business.Models.Enums;
using Business.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Business.Models;

namespace discord_bot.Interactions
{
    [IsUnit()]
	[Group("rank", "Команды для управления званиями")]
	public class RanksGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RanksGroupModule> _log;
		private readonly IGuildProviderService _guildProvider;

		public RanksGroupModule(AppDbContext db, IGuildProviderService guildProvider, ILogger<RanksGroupModule> log)
        {
			_db = db;
			_guildProvider = guildProvider;
            _log = log;
        }

		[HasPermission(PermissionType.AssignRanks)]
		[SlashCommand("up", "Повысить бойца на одно звание")]
        public async Task RankUpCommand(IUser targetedUser)
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
					_log.LogDebug($"У бойца {targetUnit.Nickname} уже самое высокое на данный момент звание: {targetUnit.Rank.Name}.");
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
				_log.LogError(ex, ex.Message);
			}
		}

		[HasPermission(PermissionType.AssignRanks)]
		[SlashCommand("set", "Установить бойцу выбранное звание")]
		public async Task SetRankCommand(IUser targetedUser, int? rankId = null)
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
					while (CurrentTestRank.Lower != null)
					{
						CurrentTestRank = CurrentTestRank.Lower;
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
						_log.LogError($"Звание c Id {rankId} не найдено.");
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
				_log.LogError(ex, ex.Message);
			}
		}

		[HasPermission(PermissionType.AssignRanks)]
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
					_log.LogError($"Боец с Id {targetId} не найден в системе.");
					return;
				}

				if (rank == null)
                {
					await RespondAsync($"Звание c Id {selectedRankIdRaw} не найдено.", ephemeral: true);
					_log.LogError($"Звание c Id {selectedRankIdRaw} не найдено.");
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
				_log.LogError($"Ошибка в RankMenuHandler: {ex.Message}");
				await RespondAsync("Ошибка при обновлении должностей.", ephemeral: true);
            }
        }

		[IsUnit()]
		[SlashCommand("list", "Вывести очередь на повышение")]
		public async Task RankListCommand()
		{
			List<Unit> units = _db.Units
				.Where(u => u.Posts.Any())
				.Where(u => u.Rank.Next != null)
				.Where(u => u.Rank.Next.CounterToReach <= u.RankUpCounter)
				.ToList();

			if (units.Any())
			{
				string unitsString = string.Join("\n", units.Select(u =>
				u.Rank.Name + " " + u.Nickname + ": " + u.RankUpCounter + '/' + u.Rank.Next.CounterToReach));

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle("Бойцы на повышение")
					.WithDescription("Эти соклановцы достигли нужного количества дней активности," +
					" чтобы получить следующее звание")
					.WithCurrentTimestamp()
					.AddField("Список:", unitsString)
					.WithFooter("Бойцов в очереди: " + units.Count());

				await RespondAsync(embed: embed.Build());
			}
			else
				await RespondAsync("Нет бойцов, ждущих повышение.");
		}
    }
}
