using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
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

			public int PostsPerPage { get; set; } = 4; // Количество должностей на одну страницу

			public PostsGroupModule(AppDbContext db, IGuildProviderService guildProvider, ILogService logService)
            {
                _db = db;
                _guildProvider = guildProvider;
                _logService = logService; 
            }

            public override Task BeforeExecuteAsync(ICommandInfo command)
            {
                _db.Posts.LoadAsync();
                _db.Units.LoadAsync();
                return base.BeforeExecuteAsync(command);
            }

            [HasPermission(PermissionType.ChangePosts)]
            [SlashCommand("set", "Назначить должности бойцу")]
            public async Task AssignPostCommand(IUser target, int? postId = null)
            {
                try
                {
                    Unit? actorUnit = await _db.Units.FindAsync(Context.User.Id);
					Unit? targetUnit = await _db.Units.FindAsync(target.Id);

                    if (targetUnit == null)
                    {
                        await RespondAsync("Боец не найден.", ephemeral: true);
                        return;
                    }

                    if (postId != null)
                    {
						var post = await _db.Posts.FindAsync(postId);
						if (post == null)
						{
							await RespondAsync($"Должность с ID {postId} не найдены.", ephemeral: true);
							await _logService.WriteAsync($"Должность с ID {postId} не найдена.", LoggingLevel.Error);
                            return;
						}

						// удаление всех текущих должностей
						foreach (Post unitPost in targetUnit.Posts)
						{
							if (unitPost.DiscordRoleId != null)
								await _guildProvider.GetGuild().GetUser(target.Id).RemoveRoleAsync((ulong)unitPost.DiscordRoleId);
							Subdivision? oldSubdiv = unitPost.Subdivision;
							while (oldSubdiv != null)
							{
								if (oldSubdiv.DiscordRoleId != null)
									await _guildProvider.GetGuild().GetUser(target.Id).RemoveRoleAsync((ulong)oldSubdiv.DiscordRoleId);
								oldSubdiv = oldSubdiv.Head;
							}
						}

						targetUnit.Posts.Clear();

						// добавление выбранной должности

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

						await _guildProvider.GetGuild().GetUser(target.Id).AddRolesAsync(roles);

						await _db.SaveChangesAsync();
						await RespondAsync("Должности обновлены.", ephemeral: true);
                        return;
					}

					List<Post> actorPosts = actorUnit.Posts;
					List<Post> allowedPosts = new List<Post>();

                    // все доступные подчинённые должности актёра (рекурсивно)
                    if (actorUnit.HasPermission(PermissionType.Administrator))
                        allowedPosts.AddRange(await _db.Posts.ToListAsync());
                    else
                        allowedPosts.AddRange(actorPosts
                            .SelectMany(p => p.GetAllSubordinatesRecursive())
                            .DistinctBy(p => p.Id)
                            .ToList());

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

            [SlashCommand("list", "Вывести список всех существующих должностей")]
            public async Task PostListCommand()
            {
                await DeferAsync(ephemeral: true);

				int postsAmount = await _db.Posts.CountAsync();
				int pagesAmount = postsAmount / PostsPerPage + (postsAmount % PostsPerPage > 0 ? 1 : 0);

				ComponentBuilder component = new ComponentBuilder();

				if (pagesAmount > 1)
					component.WithButton(">", $"view-post-list-nav-1:1");
				if (pagesAmount > 3)
					component.WithButton(">>", $"view-post-list-nav-1:3");
				if (pagesAmount > 2)
					component.WithButton(">>|", $"view-post-list-nav-1:{pagesAmount - 1}");

				await ModifyOriginalResponseAsync(func: async (options) => {
					options.Content = "";
                    options.Components = component.Build();
					options.Embed = (await CreatePostsListPageAsync(1)).Build();
				});
			}

            [SlashCommand("view", "Узнать о должности подробнее")]
            public async Task PostViewCommand(int? postId = null)
            {
                await DeferAsync(ephemeral: true);

                if (postId != null)
                    await RespondAsync(embed: (await CreatePostPageAsync((int)postId)).Build());
                else
                {
                    ComponentBuilder component = new ComponentBuilder();
                    SelectMenuBuilder selectMenu = new SelectMenuBuilder()
                        .WithCustomId("view-post-menu")
                        .WithMinValues(1).WithMaxValues(1)
                        .WithPlaceholder("Должность");

					foreach (Post post in await _db.Posts.ToListAsync())
                    {
                        selectMenu.AddOption(post.GetFullName(), post.Id.ToString(), "Людей: " + post.Units.Count.ToString());
                    }

                    await ModifyOriginalResponseAsync(func: (options) => {
                        options.Content = "Выберите должность";
                        options.Components = component.WithSelectMenu(selectMenu).Build();
                    });
                }
            }

			public async Task<EmbedBuilder> CreatePostsListPageAsync(int page) // пагинация начинается с единицы
			{
				List<Post> posts = await _db.Posts.Skip((page - 1) * PostsPerPage).Take(PostsPerPage).ToListAsync();

				EmbedBuilder embed = new EmbedBuilder()
					.WithTitle("Должности")
					.WithColor(Color.DarkGreen)
					.WithDescription("Всего должностей существует: " + await _db.Posts.CountAsync())
					.WithFooter("Страница " + page.ToString());

				foreach (Post post in posts)
				{
                    string postString = "ID: " + post.Id.ToString() +
                        $"\r\nБойцы ({post.Units.Count}): " + post.Units.Select(u => u.GetOnlyNickname());

                    if (post.Subdivision != null)
                        postString += "\r\nПодразделение: " + post.Subdivision.GetFullName();

					embed.AddField(post.GetFullName(), postString);
				}

				return embed;
			}

			public async Task<EmbedBuilder> CreatePostPageAsync(int postId)
            {
                Post? post = await _db.Posts.FindAsync(postId);
                EmbedBuilder embed = new EmbedBuilder();
                if (post != null)
                {
                    embed.WithTitle(post.GetFullName())
                        .AddField("Описание", post.Description)
                        .AddField("Разрешения", String.Join(", ", post.Permissions.Any(p => p.Type == PermissionType.Administrator)
						? post.Permissions.First(p => p.Type == PermissionType.Administrator) : post.Permissions))
                        .AddField($"Бойцы ({post.Units.Count})", String.Join(", ", post.Units.Select(u => u.GetOnlyNickname())))
                        .WithFooter("ID: " + post.Id.ToString());
                    if (post.Subdivision != null)
                        embed.WithDescription(post.Subdivision.GetFullName());
                    if (post.DiscordRoleId != null)
                        embed.WithColor((await _guildProvider.GetGuild().GetRoleAsync((ulong)post.DiscordRoleId)).Color);
                    else
                        embed.WithColor(Color.DarkGreen);
                }
                else
                    embed.WithTitle("Ошибка").WithDescription("Должность не найдена").WithColor(Color.Red);

                return embed;
            }

            [ComponentInteraction("view-post-menu", ignoreGroupNames: true)]
            public async Task ViewPostMenuHandler(string[] selectedIds)
            {
                await DeferAsync(ephemeral: true);

                if (selectedIds.Any())
                {
                    await ModifyOriginalResponseAsync(func: async (options) =>
                    {
                        options.Content = "";
                        options.Embed = (await CreatePostPageAsync(int.Parse(selectedIds.First()))).Build();
                    });
                }
                else
                {
					await ModifyOriginalResponseAsync(func: async (options) =>
					{
						options.Content = "Ошибка: не удалось получить Id должности.";
					});
				}
            }

            [ComponentInteraction("view-post-list-nav-*:*", ignoreGroupNames: true)]
            public async Task ViewPostListNavigationHandler(string prevPageString, string pageActionString)
            {
                int newPage = int.Parse(prevPageString) + int.Parse(pageActionString);
                int postsAmount = await _db.Posts.CountAsync();
                int pagesAmount = postsAmount / PostsPerPage + (postsAmount % PostsPerPage > 0 ? 1 : 0);

                ComponentBuilder component = new ComponentBuilder();

                if (newPage > 2)
                    component.WithButton("|<<", $"view-post-list-nav-{newPage}:{-newPage + 1}");
                if (newPage > 3)
                    component.WithButton("<<", $"view-post-list-nav-{newPage}:-3");
				if (newPage > 1)
					component.WithButton("<", $"view-post-list-nav-{newPage}:-1");
				if (newPage < pagesAmount)
                    component.WithButton(">", $"view-post-list-nav-{newPage}:1");
				if (newPage < pagesAmount - 3)
					component.WithButton(">>", $"view-post-list-nav-{newPage}:3");
				if (newPage < pagesAmount - 2)
					component.WithButton(">>|", $"view-post-list-nav-{newPage}:{pagesAmount - newPage}");

				await ModifyOriginalResponseAsync(func: async (options) =>
				{
					options.Components = component.Build();
					options.Embed = (await CreatePostPageAsync(newPage)).Build();
				});
			}
        }
    }
}
