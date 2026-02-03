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
    [Group("subdivision", "Команды для работы с подразделениями")]
    public class SubdivisionGropModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;

        private static readonly Dictionary<ulong, (string name, string? description, List<string> perms)> _tempData
    = new();


        public SubdivisionGropModule(AppDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }


        [HasPermission(PermissionType.ManageStructure)]
        [SlashCommand("create", "Создать подразделение")]
        public async Task CreateCommand(string name, string? description = null)
        {
            try
            {
                var unit = await _db.Units
                    .Include(u => u.GetPermissions())
                    .FirstOrDefaultAsync(u => u.DiscordId == Context.User.Id);

                if (unit == null)
                {
                    await RespondAsync("Вы не зарегистрированы в системе.", ephemeral: true);
                    return;
                }

                var permissions = unit.GetPermissions();

                var menuPermissions = new SelectMenuBuilder()
                    .WithCustomId($"subdivision-create-perms:{name}:{description}")
                    .WithPlaceholder("Выберите разрешения для подразделения")
                    .WithMinValues(0)
                    .WithMaxValues(permissions.Count);

                foreach (var perm in permissions)
                {
                    menuPermissions.AddOption(
                        perm.Type.ToString(),
                        perm.Type.ToString()
                    );
                }

                var subdivisions = await _db.Subdivisions.ToListAsync();

                var menuParent = new SelectMenuBuilder()
                    .WithCustomId($"subdivision-create-parent:{name}:{description}")
                    .WithPlaceholder("Выберите родительское подразделение (опционально)")
                    .WithMinValues(0)
                    .WithMaxValues(1);

                foreach (var sub in subdivisions)
                {
                    menuParent.AddOption(sub.Name, sub.Id.ToString());
                }

                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(menuPermissions)
                    .WithSelectMenu(menuParent);

                await RespondAsync(
                    text: "Выберите параметры для нового подразделения:",
                    components: builder.Build(),
                    ephemeral: true
                );

            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка при создании подразделения: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при создании подразделения.", ephemeral: true);
            }
        }


        [ComponentInteraction("subdivision-create-perms:*:*")]
        public async Task PermissionHandler(string name, string? description, string[] selectedPermissions)
        {
            await DeferAsync(ephemeral: true);

            string key = $"subdivision-{Context.User.Id}-{name}";
            string dir = Path.Combine("temp", "subdivision");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, $"{key}.json");

            var json = new
            {
                name,
                description,
                permissions = selectedPermissions
            };

            await File.WriteAllTextAsync(file,
                System.Text.Json.JsonSerializer.Serialize(json));

            await FollowupAsync("Разрешения сохранены. Теперь выберите родительское подразделение.", ephemeral: true);
        }


        [ComponentInteraction("subdivision-create-parent:*:*")]
        public async Task SubdivisionCreateParentHandler(string name, string? description, string[] selectedParent)
        {
            try
            {
                await DeferAsync(ephemeral: true);

                if (!_tempData.TryGetValue(Context.User.Id, out var data))
                {
                    await FollowupAsync("Ошибка: данные не найдены.", ephemeral: true);
                    return;
                }

                var subdivision = new Subdivision
                {
                    Name = data.name,
                    Description = data.description ?? string.Empty,
                    HeadId = selectedParent.Length > 0
                        ? int.Parse(selectedParent[0]) : null
                };

                await _db.Subdivisions.AddAsync(subdivision);
                await _db.SaveChangesAsync();

                foreach (var perm in data.perms)
                {
                    var permissionType = Enum.Parse<PermissionType>(perm);

                    var permission = await _db.Permissions
                        .Include(p => p.Subdivisions)
                        .FirstOrDefaultAsync(p => p.Type == permissionType);

                    if (permission != null)
                    {
                        permission.Subdivisions.Add(subdivision);
                    }
                    else
                    {
                        await _logService.WriteAsync($"Permission '{perm}' не найдено в базе данных.", LoggingLevel.Error);
                    }
                }

                await _db.SaveChangesAsync();

                _tempData.Remove(Context.User.Id);

                await FollowupAsync($"Подразделение '{subdivision.Name}' успешно создано.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка при создании подразделения: {ex.Message}", LoggingLevel.Error);
                await FollowupAsync("Ошибка при создании подразделения.", ephemeral: true);
            }
        }
    }
}
