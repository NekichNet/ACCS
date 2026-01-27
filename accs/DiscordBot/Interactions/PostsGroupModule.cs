using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace accs.DiscordBot.Interactions
{
    
    namespace accs.DiscordBot.Interactions
    {
        [IsUnit()]
        [Group("post", "Команды для управления должностями")]
        public class PostsGroupModule : InteractionModuleBase<SocketInteractionContext>
        {
            private readonly AppDbContext _db;
            private readonly IGuildProviderService _guildProvider;
            private readonly ILogService _logService;

            public PostsGroupModule(AppDbContext db, IGuildProviderService guildProvider, ILogService logService)
            {
                _db = db;
                _guildProvider = guildProvider;
                _logService = logService; 
            }

            [HasPermission(PermissionType.ChangePosts)]
            [SlashCommand("set", "Назначить должности бойцу")]
            public async Task AssignPostCommand(IUser target)
            {
                try
                {
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

			[HasPermission(PermissionType.ChangePosts)]
			[ComponentInteraction("post-menu-*", ignoreGroupNames: true)]
            public async Task PostMenuHandler(ulong targetId, string[] selectedValues)
            {
                try
                {
                    var selectedIds = selectedValues
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

                    if (targetUnit == null)
                    {
                        await RespondAsync($"Ошибка: пользователь с id {targetId} не найден в системе!", ephemeral: true);
                        await _logService.WriteAsync($"Пользователь с id {targetId} не найден в системе!", LoggingLevel.Error);
                        return;
                    }

                    // удаление всех текущих должностей
                    foreach (Post post in targetUnit.Posts)
                    {
						if (post.DiscordRoleId != null)
							await _guildProvider.GetGuild().GetUser(targetId).RemoveRoleAsync((ulong)post.DiscordRoleId);
						Subdivision? subdiv = post.Subdivision;
						while (subdiv != null)
						{
							if (subdiv.DiscordRoleId != null)
								await _guildProvider.GetGuild().GetUser(targetId).RemoveRoleAsync((ulong)subdiv.DiscordRoleId);
							subdiv = subdiv.Head;
						}
					}

					targetUnit.Posts.Clear();

                    // добавление выбранных должностей
                    foreach (var postId in selectedIds) 
                    {
                        var post = await _db.Posts.FindAsync(postId);
                        if (post != null)
                        {
							List<IRole> roles = new List<IRole>();

							targetUnit.Posts.Add(post);
							if (post.DiscordRoleId != null)
								roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId));
							Subdivision? subdiv = post.Subdivision;
							while (subdiv != null)
							{
								if (subdiv.DiscordRoleId != null)
									roles.Add(await _guildProvider.GetGuild().GetRoleAsync((ulong)subdiv.DiscordRoleId));
								subdiv = subdiv.Head;
							}

							await _guildProvider.GetGuild().GetUser(targetId).AddRolesAsync(roles);
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
