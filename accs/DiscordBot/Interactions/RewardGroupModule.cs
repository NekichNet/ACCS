using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enum;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    [IsUnit()]
    [Group("reward", "Команды для работы с наградами")]
    public class RewardGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IRewardRepository _rewardRepository;
        private IUnitRepository _unitRepository;
        private ILogService _logService;

        public RewardGroupModule(IRewardRepository rewardRepository, IUnitRepository unitRepository, ILogService logService)
        {
            _rewardRepository = rewardRepository;
            _unitRepository = unitRepository;
            _logService = logService;
        }

        [HasPermission(PermissionType.AssignRewards)]
        [SlashCommand("assign", "Присвоить награду бойцу")]
        public async Task AssignCommand(IUser user, int? rewardId = null)
        {
            Unit? unit = await _unitRepository.ReadAsync(user.Id);
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

                foreach (Reward reward in await _rewardRepository.ReadAllAsync())
                {
                    menuBuilder.AddOption(reward.Name, reward.Id.ToString(), reward.Description);
                }

				var builder = new ComponentBuilder()
					.WithSelectMenu(menuBuilder);

				await ReplyAsync("Выберите награды, которые выдать " + unit.Nickname, components: builder.Build());
			}
            else
            {
                Reward reward = await _rewardRepository.ReadAsync((int)rewardId);
                if (reward == null)
                {
                    await DeleteOriginalResponseAsync();
					await RespondAsync($"Награда с Id {rewardId} не найдена в системе", ephemeral: true);
					await _logService.WriteAsync($"Награда с Id {rewardId} не найдена в системе", LoggingLevel.Debug);
					return;
				}
                unit.Rewards.Add(reward);
                await _unitRepository.UpdateAsync(unit);
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
			Unit? unit = await _unitRepository.ReadAsync(ulong.Parse(unitId));
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
				Reward reward = await _rewardRepository.ReadAsync(int.Parse(selectedId));
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
			await _unitRepository.UpdateAsync(unit);
			await DeleteOriginalResponseAsync();
			await RespondAsync($"Бойцу {unit.Nickname} выданы награды: {String.Join(", ", rewards)}", ephemeral: true);
		}
    }
}
