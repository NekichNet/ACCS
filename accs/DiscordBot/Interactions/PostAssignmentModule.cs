using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models.Configurations;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Reactive;
namespace accs.DiscordBot.Interactions
{
    
    namespace accs.DiscordBot.Interactions
    {
        [IsUnit()]
        [HasPermission(PermissionType.ChangePosts)]
        public class PostAssignmentModule : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AppDbContext _db;
            private readonly ILogService _logService;

            public PostAssignmentModule(AppDbContext db,
                ILogService logService)
            {
                _db = db;
                _logService = logService; 
            }


            [SlashCommand("set-posts", "Назначить должности бойцу")]
            public async Task AssignPostCommand(IUser target)
            {
                try
                {
                    await _db.Units.LoadAsync(); 
                    await _db.Posts.LoadAsync();

                    var actorUnit = await _db.Units.FindAsync(Context.User.Id);
                    var targetUnit = await _db.Units.FindAsync(target.Id);

                    if (targetUnit == null)
                    {
                        await RespondAsync("Боец не найден.", ephemeral: true);
                        return;
                    }

                    var actorPosts = actorUnit.Posts;

                    // все доступные подчинённые должности актёра (рекурсивно)
                    var allowedPosts = actorPosts
                        .SelectMany(p => p.GetAllSubordinatesRecursive())
                        .DistinctBy(p => p.Id)
                        .ToList();

                    if (!allowedPosts.Any())
                    {
                        await RespondAsync("Нет доступных должностей для назначения.", ephemeral: true);
                        return;
                    }

                    var menu = new SelectMenuBuilder()
                        .WithCustomId($"post-menu-{target.Id}")
                        .WithPlaceholder("Выберите должности")
                        .WithMinValues(0)
                        .WithMaxValues(allowedPosts.Count);

                    foreach (var post in allowedPosts)
                        menu.AddOption(post.GetFullName(), post.Id.ToString());

                    var builder = new ComponentBuilder()
                        .WithSelectMenu(menu);

                    await RespondAsync(
                        $"Назначение должностей для {targetUnit.Nickname}",
                        components: builder.Build(),
                        ephemeral: true);

                }
                catch (Exception ex)
                {
                    await _logService.WriteAsync($"Ошибка в AssignRankCommand: {ex.Message}", LoggingLevel.Error);
                    await RespondAsync("Ошибка при назначении должностей.", ephemeral: true);
                }
            }


            [ComponentInteraction("post-menu-*")]
            public async Task PostMenuHandler(ulong targetId)
            {
                try
                {
                    var component = (SocketMessageComponent)Context.Interaction;

                    var selectedIds = component.Data.Values
                        .Select(v => int.Parse(v))
                        .ToList();

                    
                    for (int i = 0; i < selectedIds.Count; i++)
                    {
                        var post = await _db.Posts.FindAsync(selectedIds[i]);
                        if (post == null)
                        {
                            await RespondAsync($"должность {selectedIds[i]} не найдены.", ephemeral: true);
                            await _logService.WriteAsync($"Должность с ID {selectedIds[i]} не найдена.", LoggingLevel.Error);
                        }
                    }

                    var targetUnit = await _db.Units.FindAsync(targetId);

                    // удаление всех текущих должностей
                    targetUnit?.Posts.Clear();

                    // добавление выбранных должностей
                    foreach (var postId in selectedIds) 
                    {
                        var post = await _db.Posts.FindAsync(postId);
                        if (post != null)
                        {
                            targetUnit?.Posts.Add(post);
                        }
                        else
                        {
                            await _logService.WriteAsync($"Пост {postId} не найден.", LoggingLevel.Error);
                        } 
                    }

                    await _db.SaveChangesAsync();

                    await RespondAsync("Должности обновлены.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    await _logService.WriteAsync($"Ошибка в RankMenuHandler: {ex.Message}", LoggingLevel.Error);
                    await RespondAsync("Ошибка при обновлении должностей.", ephemeral: true);
                }
            }
        }
    }
}
