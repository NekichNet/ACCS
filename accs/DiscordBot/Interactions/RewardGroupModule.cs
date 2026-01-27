using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
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
            Unit? unit = await _db.Units.FindAsync(user.Id);
            if (unit == null)
            {
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

				await RespondAsync("Выберите награды, которые выдать " + unit.Nickname, components: builder.Build(), ephemeral: true);
			}
            else
            {
                Reward? reward = await _db.Rewards.FindAsync(rewardId.Value);
                if (reward == null)
                {
					await RespondAsync($"Награда с Id {rewardId} не найдена в системе", ephemeral: true);
					await _logService.WriteAsync($"Награда с Id {rewardId} не найдена в системе", LoggingLevel.Debug);
					return;
				}

                unit.Rewards.Add(reward);
                await _db.SaveChangesAsync();
				await RespondAsync($"Бойцу {unit.GetOnlyNickname()} выдана награда: {reward.Name}", ephemeral: true);
			}
        }

        
        [HasPermission(PermissionType.ManageRewards)]
        [SlashCommand("create", "Создать награду")]
        public async Task CreateCommand(string name, string description,
            IAttachment? image = null)
        {
            try
            {
                string? savedImagePath = null;
                if (image != null)
                {
                    var http = new HttpClient(); 
                    var bytes = await http.GetByteArrayAsync(image.Url); 

                    string rewardsDir = Path.Combine("temp", "rewards"); 
                    Directory.CreateDirectory(rewardsDir); 
                    string filePath = Path.Combine(rewardsDir, image.Filename);

                    await File.WriteAllBytesAsync(filePath, bytes); 
                    savedImagePath = filePath;
                }

                Reward reward = new Reward() 
                {
                    Name = name,
                    Description = description, 
                    ImagePath = savedImagePath 
                };

				await _db.Rewards.AddAsync(reward);
				await _db.SaveChangesAsync();

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Награда создана")
                    .WithColor(Color.Gold)
                    .WithDescription(reward.Description);

				if (image != null)
                    await RespondWithFileAsync(
                        reward.ImagePath,
                        embed: embed.WithImageUrl($"attachment://{Path.GetFileName(reward.ImagePath)}").Build(),
                        ephemeral: true
                    );
                else
                    await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка при создании награды: {ex.Message}", LoggingLevel.Error); 
                await RespondAsync("Ошибка при создании награды.", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.AssignRewards)]
		[ComponentInteraction("reward-menu-*", ignoreGroupNames: true)]
        public async Task MenuHandler(string unitId, string[] selectedIds)
        {
            Unit? unit = await _db.Units.FindAsync(ulong.Parse(unitId));
			if (unit == null)
			{
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
					await RespondAsync($"Награда с Id {selectedId} не найдена в системе", ephemeral: true);
					await _logService.WriteAsync($"Награда с Id {selectedId} не найдена в системе", LoggingLevel.Debug);
					return;
				}
				rewards.Add(reward);
			}
			unit.Rewards.AddRange(rewards);
			await _db.SaveChangesAsync();
			await RespondAsync($"Бойцу {unit.GetOnlyNickname()} выданы награды: {String.Join(", ", rewards.Select(r => r.Name))}");
		}
    }
}
