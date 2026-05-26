using accs.Database;
using accs.Models.Enums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using accs.Models.Database;

namespace discord_bot.Interactions
{
    //[IsUnit()]
    //[Group("subdivision", "Команды для работы с подразделениями")]
    public class SubdivisionGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SubdivisionGroupModule> _log;

        private static readonly Dictionary<ulong, (string name, string? description, List<string> perms)> _tempData
    = new();


        public SubdivisionGroupModule(AppDbContext db, ILogger<SubdivisionGroupModule> log)
        {
            _db = db;
            _log = log;
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[SlashCommand("create", "Создать подразделение")]
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
				_log.LogError($"Ошибка при создании подразделения: {ex.Message}");
                await RespondAsync("Ошибка при создании подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[ComponentInteraction("subdivision-create-perms:*:*")]
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


        //[HasPermission(PermissionType.ManageStructure)]
        //[ComponentInteraction("subdivision-create-parent:*:*")]
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
						_log.LogError($"Permission '{perm}' не найдено в базе данных.");
                    }
                }

                await _db.SaveChangesAsync();

                _tempData.Remove(Context.User.Id);

                await FollowupAsync($"Подразделение '{subdivision.Name}' успешно создано.", ephemeral: true);
            }
            catch (Exception ex)
            {
				_log.LogError($"Ошибка при создании подразделения: {ex.Message}");
                await FollowupAsync("Ошибка при создании подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.Administrator)]
        //[SlashCommand("delete", "Удалить подразделение")]
        public async Task DeleteCommand(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    var subdivision = await _db.Subdivisions
                        .Include(s => s.Subordinates)
                        .Include(s => s.Permissions)
                        .FirstOrDefaultAsync(s => s.Id == id.Value);

                    if (subdivision == null)
                    {
                        await RespondAsync("Подразделение не найдено.", ephemeral: true);
                        return;
                    }

                    if (subdivision.Subordinates.Any())
                    {
                        await RespondAsync("Нельзя удалить подразделение, у которого есть подчинённые.", ephemeral: true);
                        return;
                    }

                    foreach (var perm in subdivision.Permissions)
                        perm.Subdivisions.Remove(subdivision);

                    _db.Subdivisions.Remove(subdivision);
                    await _db.SaveChangesAsync();

                    await RespondAsync($"Подразделение **{subdivision.Name}** удалено.");
                    return;
                }

                // Если ID нету — показываем менюху
                var subdivisions = await _db.Subdivisions.ToListAsync();

                if (!subdivisions.Any())
                {
                    await RespondAsync("Подразделений пока нет.", ephemeral: true);
                    return;
                }

                var menu = new SelectMenuBuilder()
                    .WithCustomId("subdivision-delete-select")
                    .WithPlaceholder("Выберите подразделение для удаления")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var sub in subdivisions)
                    menu.AddOption(sub.Name, sub.Id.ToString());

                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(menu);

                await RespondAsync(
                    "Выберите подразделение для удаления:",
                    components: builder.Build(), ephemeral: true
                );
            }
            catch (Exception ex)
            {
				_log.LogError($"Ошибка в DeleteCommand: {ex.Message}");
                await RespondAsync("Ошибка при удалении подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.Administrator)]
        //[ComponentInteraction("subdivision-delete-select")]
        public async Task SubdivisionDeleteSelectHandler(string selectedId)
        {
            try
            {
                await DeferAsync(ephemeral: true);

                if (!int.TryParse(selectedId, out int id))
                {
                    await FollowupAsync("Ошибка: неверный ID подразделения.", ephemeral: true);
                    return;
                }

                var subdivision = await _db.Subdivisions
                    .Include(s => s.Subordinates)
                    .Include(s => s.Permissions)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subdivision == null)
                {
                    await FollowupAsync("Подразделение не найдено.", ephemeral: true);
                    return;
                }

                if (subdivision.Subordinates.Any())
                {
                    await FollowupAsync("Нельзя удалить подразделение, у которого есть подчинённые.", ephemeral: true);
                    return;
                }

                foreach (var perm in subdivision.Permissions)
                    perm.Subdivisions.Remove(subdivision);

                _db.Subdivisions.Remove(subdivision);
                await _db.SaveChangesAsync();

                await FollowupAsync($"Подразделение '{subdivision.Name}' успешно удалено.", ephemeral: true);
            }
            catch (Exception ex)
            {
				_log.LogError($"Ошибка в SubdivisionDeleteSelectHandler: {ex.Message}");
                await FollowupAsync("Ошибка при удалении подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[SlashCommand("edit", "Редактировать подразделение")]
        public async Task EditCommand(int id)
        {
            try
            {

            }
            catch (Exception ex)
            {
				_log.LogError($"Ошибка в EditCommand: {ex.Message}");
                await RespondAsync("Ошибка при реадктировании подразделения.", ephemeral: true);
            }
        }
    }
}
