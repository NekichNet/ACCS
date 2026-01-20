using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Configurations;
using accs.Repository;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
namespace accs.DiscordBot.Interactions
{
    
    namespace accs.DiscordBot.Interactions
    {
        [IsUnit()]
        [HasPermission(PermissionType.ChangePosts)]
        public class PostAssignmentModule : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly IPostRepository _postRepository; 
            private readonly IUnitRepository _unitRepository;
            private readonly ILogService _logService;
            //private readonly AppDbContext _context;

            public PostAssignmentModule(IPostRepository postRepository,
                IUnitRepository unitRepository,
                ILogService logService)
            {
                _postRepository = postRepository; 
                _unitRepository = unitRepository;
                _logService = logService; 
            }


            [SlashCommand("set-posts", "Назначить должности бойцу")]
            public async Task AssignPostCommand(IUser target)
            {
                try
                {
                    var actorUnit = await _unitRepository.ReadAsync(Context.User.Id);
                    var targetUnit = await _unitRepository.ReadAsync(target.Id);

                    if (targetUnit == null)
                    {
                        await RespondAsync("Боец не найден.", ephemeral: true);
                        return;
                    }

                    var actorPosts = actorUnit.Posts;

                    // все доступные подчинённые должности актёра (рекурсивно)
                    var allowedPosts = actorPosts
                        .SelectMany(p => p.GetAllSubordinatesRecursive())   // рекурсивно собираю всех  этого поста
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
                        var post = await _postRepository.ReadAsync(selectedIds[i]);
                        if (post == null)
                        {
                            await RespondAsync($"должность {selectedIds[i]} не найдены.", ephemeral: true);
                            await _logService.WriteAsync($"Должность с ID {selectedIds[i]} не найдена.", LoggingLevel.Error);
                        }
                    }

                    var targetUnit = await _unitRepository.ReadAsync(Context.User.Id);

                    // удаление всех текущих должностей
                    var unit = await _context.Units
                        .Include(u => u.Posts)
                        .FirstAsync(u => u.DiscordId == targetId);

                    unit.Posts.Clear();
                    await _context.SaveChangesAsync();

                    // добавление выбранных должностей
                    var unit2 = await _context.Units
                        .Include(u => u.Posts)
                        .FirstAsync(u => u.DiscordId == targetId);

                    foreach (var postId in selectedIds)
                    {
                        var post = await _context.Posts.FindAsync(postId);
                        if (post != null)
                            unit.Posts.Add(post);
                    }

                    await _context.SaveChangesAsync();


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
