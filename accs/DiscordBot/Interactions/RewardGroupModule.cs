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
					string description = reward.Description.Substring(0, 95);
					menuBuilder.AddOption(reward.Name, reward.Id.ToString(),
						description.Length == 95 ? description + "..." : description);
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
        public async Task CreateCommand(string name, string description, IAttachment? image = null)
        {
            try
            {
                string? savedImagePath = null;
                if (image != null)
                {
                    var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(image.Url);

                    if (!Directory.Exists("rewards"))
                        Directory.CreateDirectory("rewards");
                    string filePath = Path.Join("rewards", image.Filename);

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
                    .WithTitle($"Награда {reward.Name} создана")
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
        [ComponentInteraction("menu-*", ignoreGroupNames: true)]
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
                embed.AddField(reward.Name, $"ID: {reward.Id}\r\n" + reward.Description);

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
                    reward.Description = description;
                    changed = true;
                }

                if (image != null)
                {
                    if (!image.ContentType.StartsWith("image"))
                    {
                        await RespondAsync("Файл должен быть изображением.", ephemeral: true);
                        return;
                    }

                    if (!Directory.Exists("rewards"))
                    {
                        Directory.CreateDirectory("rewards");
                    }

                    string filePath = Path.Combine("rewards", image.Filename);

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
                await _logService.WriteAsync($"Error in EditCommand: {ex.Message}", LoggingLevel.Error);
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

                    foreach (var reward in rewards)
                    {
                        string description = reward.Description.Substring(0, 95);
						menu.AddOption(reward.Name, reward.Id.ToString(),
                            description.Length == 95 ?description + "..." : description);
                    }

                    ComponentBuilder builder = new ComponentBuilder()
                        .WithSelectMenu(menu);

                    await RespondAsync("Выберите награду для удаления:", components: builder.Build(), ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Error in DeleteCommand: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при удалении награды.", ephemeral: true);
            }
        }


        [HasPermission(PermissionType.ManageRewards)]
        [ComponentInteraction("reward-delete-select")]
        public async Task RewardDeleteHandler(string selectedId)
        {
            try
            {
                if (!int.TryParse(selectedId, out int id))
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
                await _logService.WriteAsync($"Error in RewardDeleteSelectHandler: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при удалении награды.", ephemeral: true);
            }
        }
    }
}