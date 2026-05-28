using accs.Database;
using discord_bot.Preconditions;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using accs.Models;

namespace discord_bot.Interactions
{
    [IsUnit()]
    [Group("reward", "Команды для работы с наградами")]
    public class RewardGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RewardGroupModule> _log;
		private readonly IGuildProviderService _guildProvider;

		public RewardGroupModule(AppDbContext db, ILogger<RewardGroupModule> log, IGuildProviderService guildProvider)
        {
            _db = db;
            _log = log;
            _guildProvider = guildProvider;
        }

        public override Task BeforeExecuteAsync(ICommandInfo command)
        {
            _db.Rewards.Load();
            return base.BeforeExecuteAsync(command);
        }

        [HasPermission(PermissionType.AssignRewards)]
        [SlashCommand("assign", "Присвоить награду бойцу")]
        public async Task AssignCommand(IUser user, int? rewardId = null)
        {
            Unit? unit = await _db.Units.FindAsync(user.Id);
            if (unit == null)
            {
                await RespondAsync($"Пользователь {user.Username} не найден в системе", ephemeral: true);
				_log.LogError($"Пользователь {user.Username} не найден в системе");
                return;
            }

            if (rewardId == null)
            {
                string customId = $"reward-menu-{unit.DiscordId}:1";

				var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Награда")
                    .WithCustomId(customId)
                    .WithMinValues(1)
                    .WithMaxValues(1);

                var rewards = await _db.Rewards.ToListAsync();

                for (int i = 0; i < rewards.Count; i++)
                {
					string description = rewards[i].Conditions.Length > 95 ? rewards[i].Conditions.Substring(0, 95) : rewards[i].Conditions;
					if (description.Length < 2)
						description = "Нет описания";
					menuBuilder.AddOption(rewards[i].Name, rewards[i].Id.ToString(),
						description.Length == 95 ? description + "..." : description);
                    if (i == 23)
                    {
                        menuBuilder.AddOption("Следующая страница", $"next-page");
                        break;
                    }
				}

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                await RespondAsync("(Страница 1) Выберите награду" + unit.Nickname, components: builder.Build(), ephemeral: true);
            }
            else
            {
                Reward? reward = await _db.Rewards.FindAsync(rewardId.Value);
                if (reward == null)
                {
                    await RespondAsync($"Награда с Id {rewardId} не найдена в системе", ephemeral: true);
					_log.LogDebug($"Награда с Id {rewardId} не найдена в системе");
                    return;
                }

                unit.Rewards.Add(reward);

                SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(user.Id);
                await guildUser.AddRoleAsync(reward.DiscordRoleId);

                await _db.SaveChangesAsync();
                await RespondAsync($"Бойцу {unit.GetOnlyNickname()} выдана награда: {reward.Name}", ephemeral: true);
            }
        }

        [HasPermission(PermissionType.ManageRewards)]
        [SlashCommand("create", "Создать награду")]
        public async Task CreateCommand(string name, string description, IAttachment? image = null)
        {
            try
            {
                string? savedImagePath = null;
                if (image != null)
                {
                    var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(image.Url);

                    if (!Directory.Exists("newRewards"))
                        Directory.CreateDirectory("newRewards");
                    string filePath = Path.Join("newRewards", image.Filename);

                    await File.WriteAllBytesAsync(filePath, bytes);
                    savedImagePath = filePath;
                }

                RestRole role = await _guildProvider.GetGuild().CreateRoleAsync(name: name, color: Color.Gold);

                Reward reward = new Reward()
                {
                    Name = name,
                    Conditions = description,
                    DiscordRoleId = role.Id,
                    ImagePath = savedImagePath
                };

                await _db.Rewards.AddAsync(reward);
                await _db.SaveChangesAsync();

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle($"Награда {reward.Name} создана")
                    .WithColor(Color.Gold)
                    .WithDescription(reward.Conditions);

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
				_log.LogError($"Ошибка при создании награды: {ex.Message}");
                await RespondAsync("Ошибка при создании награды.", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.AssignRewards)]
        [ComponentInteraction("reward-menu-*:*", ignoreGroupNames: true)]
        public async Task MenuHandler(string unitId, string pageString, string[] selectedIds)
        {
            if (selectedIds[0] == "next-page")
            {
                int page = int.Parse(pageString);

				string customId = $"reward-menu-{unitId}:{page + 1}";

				var menuBuilder = new SelectMenuBuilder()
					.WithPlaceholder("Награда")
					.WithCustomId(customId)
					.WithMinValues(1)
					.WithMaxValues(1);

				var newRewards = await _db.Rewards.ToListAsync();

				for (int i = 24 * page; i < newRewards.Count; i++)
				{
					string description = newRewards[i].Conditions.Length > 95 ? newRewards[i].Conditions.Substring(0, 95) : newRewards[i].Conditions;
					if (description.Length < 2)
						description = "Нет описания";
					menuBuilder.AddOption(newRewards[i].Name, newRewards[i].Id.ToString(),
						description.Length == 95 ? description + "..." : description);
					if (i == 24 * int.Parse(pageString) + 23)
					{
						menuBuilder.AddOption("Следующая страница", customId + $"next-page");
						break;
					}
				}

				var builder = new ComponentBuilder()
					.WithSelectMenu(menuBuilder);

                await RespondAsync($"(Страница {page + 1}) Выберите награду", components: builder.Build(), ephemeral: true);
                return;
			}

            Unit? unit = await _db.Units.FindAsync(ulong.Parse(unitId));
            if (unit == null)
            {
                await RespondAsync($"Пользователь с Id {unitId} не найден в системе", ephemeral: true);
				_log.LogDebug($"Пользователь с Id {unitId} не найден в системе");
                return;
            }

            List<Reward> rewards = new List<Reward>();

			SocketGuildUser guildUser = _guildProvider.GetGuild().GetUser(ulong.Parse(unitId));

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
					_log.LogDebug($"Награда с Id {selectedId} не найдена в системе");
                    return;
                }
                rewards.Add(reward);
				await guildUser.AddRoleAsync(reward.DiscordRoleId);
			}
            unit.Rewards.AddRange(rewards);
            await _db.SaveChangesAsync();
            await RespondAsync($"Бойцу {unit.GetOnlyNickname()} выданы награды: {string.Join(", ", rewards.Select(r => r.Name))}");
        }


        [SlashCommand("list", "Список наград")]
        public async Task RewardListCommand(int page = 1)
        {
            const int pageSize = 5;

            var rewards = await _db.Rewards.ToListAsync();

            if (!rewards.Any())
            {
                await RespondAsync("Наград пока нет.", ephemeral: true);
                return;
            }

            int totalPages = (int)Math.Ceiling(rewards.Count / (double)pageSize);
            page = Math.Clamp(page, 1, totalPages);

            var pageItems = rewards
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle($"Награды")
                .WithColor(Color.Gold)
                .WithFooter($"Страница {page}/{totalPages}");

            foreach (var reward in pageItems)
                embed.AddField(reward.Name,
                    $"ID: {reward.Id}\n"
                    + reward.Conditions
                    + "\nНаграждённые бойцы:\n"
                    + string.Join("\n", reward.Units.Select(u => u.GetOnlyNickname())));

            ComponentBuilder components = new ComponentBuilder();

            if (page > 1)
                components.WithButton("<", $"{page - 1}", ButtonStyle.Primary);
            if (page < totalPages)
                components.WithButton(">", $"{page + 1}", ButtonStyle.Primary);

            await RespondAsync(embed: embed.Build(), components: components.Build());
        }


        [ComponentInteraction("reward-list-*")]
        public async Task RewardListHandler(int page)
        {
            await DeferAsync();
            await RewardListCommand(page);
        }


        [HasPermission(PermissionType.ManageRewards)]
        [SlashCommand("edit", "Редактировать награду")]
        public async Task EditCommand(string? name = null, string? description = null, IAttachment? image = null)
        {
            try
            {
                Reward? reward = await _db.Rewards
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());

                if (reward == null)
                {
                    await RespondAsync("Награда с таким названием не найдена.", ephemeral: true);
                    return;
                }

                bool changed = false;

                if (!string.IsNullOrWhiteSpace(description))
                {
                    reward.Conditions = description;
                    changed = true;
                }

                if (image != null)
                {
                    if (!image.ContentType.StartsWith("image"))
                    {
                        await RespondAsync("Файл должен быть изображением.", ephemeral: true);
                        return;
                    }

                    if (!Directory.Exists("newRewards"))
                    {
                        Directory.CreateDirectory("newRewards");
                    }

                    string filePath = Path.Combine("newRewards", image.Filename);

                    using (var http = new HttpClient())
                    {
                        var bytes = await http.GetByteArrayAsync(image.Url);
                        await File.WriteAllBytesAsync(filePath, bytes);
                    }

                    reward.ImagePath = filePath;
                    changed = true;
                }

                if (!changed)
                {
                    await RespondAsync("Вы не указали ни одного параметра для изменения.", ephemeral: true);
                    return;
                }

                await _db.SaveChangesAsync();

                await RespondAsync($"Награда '{reward.Name}' успешно обновлена.");
            }
            catch (Exception ex)
            {
				_log.LogError($"Error in EditCommand: {ex.Message}");
                await RespondAsync("Ошибка при редактировании награды.", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.ManageRewards)]
        [SlashCommand("delete", "Удалить награду")]
        public async Task DeleteCommand(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    var reward = await _db.Rewards.FindAsync(id.Value);
                    if (reward == null)
                    {
                        await RespondAsync("Награда не найдена.", ephemeral: true);
                        return;
                    }

                    _db.Rewards.Remove(reward);
                    await _db.SaveChangesAsync();

                    await RespondAsync($"Награда '{reward.Name}' удалена.");
                    return;
                }
                else
                {
                    // Если ID нету — показываем менюху
                    var rewards = await _db.Rewards.ToListAsync();

                    if (!rewards.Any())
                    {
                        await RespondAsync("Наград пока нет.", ephemeral: true);
                        return;
                    }

                    var menu = new SelectMenuBuilder()
                        .WithCustomId("reward-delete-select")
                        .WithPlaceholder("Выберите награду для удаления");

					for (int i = 0; i < rewards.Count; i++)
					{
						string description = rewards[i].Conditions.Length > 95 ? rewards[i].Conditions.Substring(0, 95) : rewards[i].Conditions;
						if (description.Length < 2)
							description = "Нет описания";
						menu.AddOption(rewards[i].Name, rewards[i].Id.ToString(),
							description.Length == 95 ? description + "..." : description);
						if (i == 23)
						{
							menu.AddOption("Следующая страница", "next-page");
							break;
						}
					}

					ComponentBuilder builder = new ComponentBuilder()
                        .WithSelectMenu(menu);

                    await RespondAsync("Выберите награду для удаления:", components: builder.Build(), ephemeral: true);
                }
            }
            catch (Exception ex)
            {
				_log.LogError($"Error in DeleteCommand: {ex.Message}");
                await RespondAsync("Ошибка при удалении награды.", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.ManageRewards)]
        [ComponentInteraction("reward-delete-select:*")]
        public async Task RewardDeleteHandler(string pageString, string[] selectedIds)
        {
            try
            {
				if (selectedIds[0] == "next-page")
				{
					int page = int.Parse(pageString);

					string customId = $"reward-delete-select:{page + 1}";

					var menuBuilder = new SelectMenuBuilder()
						.WithPlaceholder("Награда")
						.WithCustomId(customId)
						.WithMinValues(1)
						.WithMaxValues(1);

					var newRewards = await _db.Rewards.ToListAsync();

					for (int i = 24 * page; i < newRewards.Count; i++)
					{
						string description = newRewards[i].Conditions.Length > 95 ? newRewards[i].Conditions.Substring(0, 95) : newRewards[i].Conditions;
						if (description.Length < 2)
							description = "Нет описания";
						menuBuilder.AddOption(newRewards[i].Name, newRewards[i].Id.ToString(),
							description.Length == 95 ? description + "..." : description);
						if (i == 24 * int.Parse(pageString) + 23)
						{
							menuBuilder.AddOption("Следующая страница", customId + $"next-page");
							break;
						}
					}

					var builder = new ComponentBuilder()
						.WithSelectMenu(menuBuilder);

					await ModifyOriginalResponseAsync(func: (opt) =>
					{
						opt.Components = builder.Build();
					});
					return;
				}

				if (!int.TryParse(selectedIds[0], out int id))
                {
                    await RespondAsync("Ошибка: неверный ID награды.", ephemeral: true);
                    return;
                }

                var reward = await _db.Rewards.FindAsync(id);

                if (reward == null)
                {
                    await RespondAsync("Награда не найдена.", ephemeral: true);
                    return;
                }

                _db.Rewards.Remove(reward);
                await _db.SaveChangesAsync();

                await RespondAsync($"Награда '{reward.Name}' успешно удалена.", ephemeral: true);
            }
            catch (Exception ex)
            {
				_log.LogError($"Error in RewardDeleteSelectHandler: {ex.Message}");
                await RespondAsync("Ошибка при удалении награды.", ephemeral: true);
            }
        }
    }
}