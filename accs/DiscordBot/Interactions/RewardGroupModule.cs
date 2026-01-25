using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [Group("reward", "Команды для работы с наградами")]
    public class RewardGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;

        public RewardGroupModule(AppDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        [HasPermission(PermissionType.AssignRewards)]
        [SlashCommand("assign", "Присвоить награду бойцу")]
        public async Task AssignCommand(IUser user, int? rewardId = null)
        {
            await _db.Units.LoadAsync(); 
            await _db.Rewards.LoadAsync();

            Unit? unit = await _db.Units.FindAsync(user.Id);
            if (unit == null)
            {
                await DeleteOriginalResponseAsync();
                await RespondAsync($"Пользователь {user.Username} не найден в системе", ephemeral: true);
                await _logService.WriteAsync($"Пользователь {user.Username} не найден в системе", LoggingLevel.Debug);
                return;
            }

            if (rewardId == null)
            {
				var menuBuilder = new SelectMenuBuilder()
		            .WithPlaceholder("Награда")
		            .WithCustomId($"reward-menu-{unit.DiscordId}")
		            .WithMinValues(1);

                var rewards = await _db.Rewards.ToListAsync();

                foreach (Reward reward in rewards)
                {
                    menuBuilder.AddOption(reward.Name, reward.Id.ToString(), reward.Description);
                }

				var builder = new ComponentBuilder()
					.WithSelectMenu(menuBuilder);

				await ReplyAsync("Выберите награды, которые выдать " + unit.Nickname, components: builder.Build());
			}
            else
            {
                Reward? reward = await _db.Rewards.FindAsync(rewardId.Value);
                if (reward == null)
                {
                    await DeleteOriginalResponseAsync();
					await RespondAsync($"Награда с Id {rewardId} не найдена в системе", ephemeral: true);
					await _logService.WriteAsync($"Награда с Id {rewardId} не найдена в системе", LoggingLevel.Debug);
					return;
				}

                unit.Rewards.Add(reward);
                await _db.SaveChangesAsync();
                await DeleteOriginalResponseAsync();
				await RespondAsync($"Бойцу {unit.Nickname} выдана награда: {reward.Name}", ephemeral: true);
			}
        }

        [HasPermission(PermissionType.ManageRewards)]
		public async Task CreateCommand()
        {

        }

		[HasPermission(PermissionType.AssignRewards)]
		[ComponentInteraction("reward-menu-*")]
        public async Task MenuHandler(string unitId, string[] selectedIds)
        {
            await _db.Units.LoadAsync();
            await _db.Rewards.LoadAsync();

            Unit? unit = await _db.Units.FindAsync(ulong.Parse(unitId));
			if (unit == null)
			{
				await DeleteOriginalResponseAsync();
				await RespondAsync($"Пользователь с Id {unitId} не найден в системе", ephemeral: true);
				await _logService.WriteAsync($"Пользователь с Id {unitId} не найден в системе", LoggingLevel.Debug);
				return;
			}

            List<Reward> rewards = new List<Reward>();

            foreach (string selectedId in selectedIds)
            {
                if (!int.TryParse(selectedId, out int rewardId)) 
                {
                    await RespondAsync($"Некорректный ID награды: {selectedId}", ephemeral: true); 
                    return; 
                }

                Reward? reward = await _db.Rewards.FindAsync(rewardId);
				if (reward == null)
				{
					await DeleteOriginalResponseAsync();
					await RespondAsync($"Награда с Id {selectedId} не найдена в системе", ephemeral: true);
					await _logService.WriteAsync($"Награда с Id {selectedId} не найдена в системе", LoggingLevel.Debug);
					return;
				}
				rewards.Add(reward);
			}
			unit.Rewards.AddRange(rewards);
			await _db.SaveChangesAsync();
			await DeleteOriginalResponseAsync();
			await RespondAsync($"Бойцу {unit.Nickname} выданы награды: {String.Join(", ", rewards)}", ephemeral: true);
		}
    }
}
