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
    //[IsUnit()]
    //[Group("subdivision", "Команды для работы с подразделениями")]
    public class SubdivisionGropModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;

        private static readonly Dictionary<ulong, (string name, string? description, List<string> perms)> _tempData = new();\

        private static readonly Dictionary<ulong, (int id, List<string> perms)> _tempEditData = new();

        public SubdivisionGropModule(AppDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
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
                await _logService.WriteAsync($"Ошибка при создании подразделения: {ex.Message}", LoggingLevel.Error);
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
        public async Task CreateParentHandler(string name, string? description, string[] selectedParent)
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
                await _logService.WriteAsync($"Ошибка в DeleteCommand: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при удалении подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.Administrator)]
        //[ComponentInteraction("subdivision-delete-select")]
        public async Task DeleteSelectHandler(string selectedId)
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
                await _logService.WriteAsync($"Ошибка в SubdivisionDeleteSelectHandler: {ex.Message}", LoggingLevel.Error);
                await FollowupAsync("Ошибка при удалении подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[SlashCommand("edit", "Редактировать подразделение")]
        public async Task EditCommand(int id, string? name = null, string? description = null)
        {
            try
            {
                var subdivision = await _db.Subdivisions
                    .Include(s => s.Permissions)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subdivision == null)
                {
                    await RespondAsync("Подразделение не найдено.", ephemeral: true);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(name))
                    subdivision.Name = name;

                if (!string.IsNullOrWhiteSpace(description))
                    subdivision.Description = description;

                await _db.SaveChangesAsync();

                var unit = await _db.Units
                    .Include(u => u.GetPermissions())
                    .FirstOrDefaultAsync(u => u.DiscordId == Context.User.Id);

                if (unit == null)
                {
                    await RespondAsync("Вы не зарегистрированы в системе.", ephemeral: true);
                    return;
                }

                var permissions = unit.GetPermissions();

                var permMenu = new SelectMenuBuilder()
                    .WithCustomId($"subdivision-edit-perms:{id}")
                    .WithPlaceholder("Выберите разрешения (опционально)")
                    .WithMinValues(0)
                    .WithMaxValues(permissions.Count);

                foreach (var perm in permissions)
                {
                    bool isCurrent = subdivision.Permissions.Any(p => p.Type == perm.Type);

                    permMenu.AddOption(
                        perm.Type.ToString(),
                        perm.Type.ToString(),
                        isCurrent ? "Текущее" : null
                    );
                }

                var subdivisions = await _db.Subdivisions.ToListAsync();

                var parentMenu = new SelectMenuBuilder()
                    .WithCustomId($"subdivision-edit-parent:{id}")
                    .WithPlaceholder("Выберите родительское подразделение (опционально)")
                    .WithMinValues(0)
                    .WithMaxValues(1);

                foreach (var sub in subdivisions.Where(s => s.Id != id))
                    parentMenu.AddOption(sub.Name, sub.Id.ToString());

                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(permMenu)
                    .WithSelectMenu(parentMenu);

                await RespondAsync(
                    $"Редактирование подразделения '{subdivision.Name}'.\n" +
                    "Вы можете изменить разрешения и родительское подразделение (оба параметра опциональны).",
                    components: builder.Build(),
                    ephemeral: true
                );

            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка в EditCommand: {ex.Message}", LoggingLevel.Error);
                await RespondAsync("Ошибка при реадктировании подразделения.", ephemeral: true);
            }
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[ComponentInteraction("subdivision-edit-perms:*")]
        public async Task EditPermissionsHandler(int id, string[] selectedPermissions)
        {
            await DeferAsync(ephemeral: true); 
            _tempEditData[Context.User.Id] = (id, selectedPermissions.ToList());

            await RespondAsync("Разрешения сохранены. Теперь при необходимости выберите родительское подразделение.", ephemeral: true);
        }


        //[HasPermission(PermissionType.ManageStructure)]
        //[ComponentInteraction("subdivision-edit-parent:*")]
        public async Task EditParentHandler(int id, string[] selectedParent)
        {
            try
            {
                await DeferAsync(ephemeral: true);

                var subdivision = await _db.Subdivisions
                    .Include(s => s.Permissions)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subdivision == null)
                {
                    await FollowupAsync("Подразделение не найдено.", ephemeral: true);
                    return;
                }

                subdivision.HeadId = selectedParent.Length > 0
                    ? int.Parse(selectedParent[0])
                    : null;

                if (_tempEditData.TryGetValue(Context.User.Id, out var data) && data.id == id)
                {
                    subdivision.Permissions.Clear();

                    foreach (var perm in data.perms)
                    {
                        var permissionType = Enum.Parse<PermissionType>(perm);

                        var permission = await _db.Permissions
                            .Include(p => p.Subdivisions)
                            .FirstOrDefaultAsync(p => p.Type == permissionType);

                        if (permission != null)
                            permission.Subdivisions.Add(subdivision);
                    }

                    _tempEditData.Remove(Context.User.Id);
                }
                await _db.SaveChangesAsync();

                await FollowupAsync($"Подразделение '{subdivision.Name}' успешно обновлено.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка в EditParentHandler: {ex.Message}", LoggingLevel.Error); 
                await RespondAsync("Ошибка при редактировании подразделения.", ephemeral: true);
            }
        }
    }
}
