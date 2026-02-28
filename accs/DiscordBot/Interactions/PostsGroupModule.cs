using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
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

            public override Task BeforeExecuteAsync(ICommandInfo command)
            {
                _db.Posts.Load();
                _db.Units.Load();
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

                    if (actorUnit == null)
                    {
						await RespondAsync("Вы не найдены в системе.", ephemeral: true);
						return;
					}

                    if (actorUnit.Posts.SelectMany(p => p.GetAllHeadsRecursive()).Intersect(targetUnit.Posts).Any())
                    {
						await RespondAsync("Вы не можете поменять должности этого бойца." +
                            " Одна из его должностей главенствует над Вашей.", ephemeral: true);
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

                        await post.NotifyOnAssignAsync(Context.Guild, _db, targetUnit);
                        return;
					}

					List<Post> actorPosts = actorUnit.Posts;
					List<Post> allowedPosts = new List<Post>();

                    // все доступные подчинённые должности актёра (рекурсивно)
                    if (actorUnit.HasPermission(PermissionType.Administrator))
                        allowedPosts.AddRange(await _db.Posts.ToListAsync());
                    else
                        allowedPosts.AddRange(_db.Posts.Except(actorPosts.SelectMany(p => p.GetAllHeadsRecursive())));

					if (!allowedPosts.Any())
                    {
                        await RespondAsync("Нет доступных должностей для назначения.", ephemeral: true);
                        return;
                    }

                    var menu = new SelectMenuBuilder()
                        .WithCustomId($"post-menu-{target.Id}-{actorUnit.DiscordId}:1")
                        .WithPlaceholder("Должности")
                        .WithMinValues(1)
                        .WithMaxValues(1);

					for (int i = 0; i < allowedPosts.Count; i++)
					{
						string description = allowedPosts[i].Description.Length > 95 ? allowedPosts[i].Description.Substring(0, 95) : allowedPosts[i].Description;
						if (description.Length < 2)
							description = "Нет описания";
						menu.AddOption(allowedPosts[i].GetFullName(), allowedPosts[i].Id.ToString(),
							description.Length == 95 ? description + "..." : description);
						if (i == 23)
						{
							menu.AddOption("Следующая страница", $"next-page");
							break;
						}
					}

					var builder = new ComponentBuilder()
                        .WithSelectMenu(menu);

                    await RespondAsync(
                        $"Выберите должность",
                        components: builder.Build(),
                        ephemeral: true);

                }
                catch (Exception ex)
                {
                    await _logService.WriteAsync($"Ошибка в AssignPostCommand: {ex.Message}", LoggingLevel.Error);
                    await RespondAsync("Ошибка при назначении должностей.", ephemeral: true);
                }
            }

			[HasPermission(PermissionType.ChangePosts)]
			[ComponentInteraction("post-menu-*-*:*", ignoreGroupNames: true)]
            public async Task PostMenuHandler(string targetIdString, string actorIdString, string pageString, string[] selectedValues)
            {
				if (selectedValues[0] == "next-page")
				{
					int page = int.Parse(pageString);

					string customId = $"post-menu-{targetIdString}-{actorIdString}:{page + 1}";

					var menuBuilder = new SelectMenuBuilder()
						.WithPlaceholder("Должности")
						.WithCustomId(customId)
						.WithMinValues(1)
						.WithMaxValues(1);

                    Unit? actorUnit = (await _db.Units.FindAsync(ulong.Parse(actorIdString)));

                    if (actorUnit == null)
                    {
                        await RespondAsync("Ошибка: вы не найдены в системе!", ephemeral: true);
                        await _logService.WriteAsync($"Боец с ID {actorIdString} не найден в базе данных!", LoggingLevel.Error);
                        return;
                    }

					List<Post> actorPosts = actorUnit.Posts;
					List<Post> allowedPosts = new List<Post>();

					if (actorUnit.HasPermission(PermissionType.Administrator))
						allowedPosts.AddRange(await _db.Posts.ToListAsync());
					else
						allowedPosts.AddRange(_db.Posts.Except(actorPosts.SelectMany(p => p.GetAllHeadsRecursive())));

					for (int i = 24 * page; i < allowedPosts.Count; i++)
					{
						string description = allowedPosts[i].Description.Length > 95 ? allowedPosts[i].Description.Substring(0, 95) : allowedPosts[i].Description;
                        if (description.Length < 2)
                            description = "Нет описания";
						menuBuilder.AddOption(allowedPosts[i].GetFullName(), allowedPosts[i].Id.ToString(),
							description.Length == 95 ? description + "..." : description);
						if (i == 24 * int.Parse(pageString) + 23)
						{
							menuBuilder.AddOption("Следующая страница", customId + $"next-page");
							break;
						}
					}

					var builder = new ComponentBuilder()
						.WithSelectMenu(menuBuilder);

					await RespondAsync($"(Страница {page + 1}) Выберите должность", components: builder.Build(), ephemeral: true);
					return;
				}

				try
                {
                    ulong targetId = ulong.Parse(targetIdString);

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

							await post.NotifyOnAssignAsync(Context.Guild, _db, targetUnit);
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

            //[SlashCommand("list", "Вывести список всех существующих должностей")]
            public async Task PostListCommand()
            {
                await DeferAsync(ephemeral: true);

				int postsAmount = await _db.Posts.CountAsync();
				int pagesAmount = postsAmount / 4 + (postsAmount % 4 > 0 ? 1 : 0);

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

            //[SlashCommand("view", "Узнать о должности подробнее")]
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
				List<Post> posts = await _db.Posts.Skip((page - 1) * 4).Take(4).ToListAsync();

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
                int pagesAmount = postsAmount / 4 + (postsAmount % 4 > 0 ? 1 : 0);

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

            [HasPermission(PermissionType.ManageStructure)]
            [SlashCommand("create", "Создать новую должность.")] 
            public async Task PostCreateCommand(string name, string? description = null) // Praise the code
            {
                var unit = _db.Units.Find(Context.User.Id);
                if (unit != null)
                {
                    Post post = new Post() { Name = name, Description = description == null ? string.Empty : description };

                    await _db.Posts.AddAsync(post);
                    await _db.SaveChangesAsync();

                    var permissionMenuBuilder = new SelectMenuBuilder().WithCustomId($"post-edit-{post.Id}").WithPlaceholder("Разрешения").WithMinValues(0).WithMaxValues(unit.GetPermissions().Count);
                    foreach (var permission in unit.GetPermissions()) 
                    {
                        permissionMenuBuilder.AddOption(label: permission.Type.ToString(), value: ((int)permission.Type).ToString(), description: permission.Description);
                    }
                    var selectMenuSpawner = new ComponentBuilder().WithSelectMenu(permissionMenuBuilder);
                    await RespondAsync("Выберите разрешения доступные этой должности.", components: selectMenuSpawner.Build(), ephemeral: true);
                }
                else
                {
                    var err = $"Пользователь с Id {Context.User.Id} вызвавший метод не найден в базе данных!";
                    await _logService.WriteAsync(err, LoggingLevel.Error);
                    await RespondAsync(err, ephemeral:true);
                }
            }

            [ComponentInteraction("post-edit-*", ignoreGroupNames: true)]
            public async Task PostPermissionChoice(string postId, string[] selectedPermissions) // Praise the code
            {
                var post = await _db.Posts.FindAsync(int.Parse(postId));
                if (post != null)
                {
                    foreach (var permissionIdString in selectedPermissions)
                    {
                        int permissionId = int.Parse(permissionIdString);
                        var permission = await _db.Permissions.FindAsync((PermissionType)permissionId);
                        if (permission != null)
                        {
                            post.Permissions.Add(permission);
                        }
                    }
                    _db.Posts.Update(post);
                    await _db.SaveChangesAsync();
                    await RespondAsync($"У должности {post.Name} были обновлены разрешения.", ephemeral: true);
                }
                else
                {
                    await RespondAsync("Ошибкa: Не удалось найти должность в системе.");
                }
            }


            /// <summary>
            /// Autocomplete for post searching!
            /// </summary>
            /// <returns></returns>
            [AutocompleteCommand("posts_autocomplete", "edit-name")]
            public async Task AutocompleteForPostNameEdit() // Praise the code
            {
                string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
                List<AutocompleteResult> searchRes = new List<AutocompleteResult>();
                foreach (var posts in _db.Posts)
                {
                    searchRes.Add(new AutocompleteResult() { Name = posts.GetFullName(), Value = posts.Id });
                }
                IEnumerable<AutocompleteResult> results = searchRes;
                // max - 25 suggestions at a time
                await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
            }

			/// <summary>
			/// Autocomplete for post searching!
			/// </summary>
			/// <returns></returns>
			[AutocompleteCommand("posts_autocomplete", "edit-desctription")]
			public async Task AutocompleteForPostDescriptionEdit() // Praise the code
			{
				string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
				List<AutocompleteResult> searchRes = new List<AutocompleteResult>();
				foreach (var posts in _db.Posts)
				{
					searchRes.Add(new AutocompleteResult() { Name = posts.GetFullName(), Value = posts.Id });
				}
				IEnumerable<AutocompleteResult> results = searchRes;
				// max - 25 suggestions at a time
				await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
			}

			/// <summary>
			/// Autocomplete for post searching!
			/// </summary>
			/// <returns></returns>
			[AutocompleteCommand("posts_autocomplete", "edit-permissions")]
			public async Task AutocompleteForPostPermissionEdit() // Praise the code
			{
				string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
				List<AutocompleteResult> searchRes = new List<AutocompleteResult>();
				foreach (var posts in _db.Posts)
				{
					searchRes.Add(new AutocompleteResult() { Name = posts.GetFullName(), Value = posts.Id });
				}
				IEnumerable<AutocompleteResult> results = searchRes;
				// max - 25 suggestions at a time
				await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
			}

			/// <summary>
			/// Autocomplete for post searching!
			/// </summary>
			/// <returns></returns>
			[AutocompleteCommand("posts_autocomplete", "delete")]
			public async Task AutocompleteForPostDelete() // Praise the code
			{
				string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
				List<AutocompleteResult> searchRes = new List<AutocompleteResult>();
				foreach (var posts in _db.Posts)
				{
					searchRes.Add(new AutocompleteResult() { Name = posts.GetFullName(), Value = posts.Id });
				}
				IEnumerable<AutocompleteResult> results = searchRes;
				// max - 25 suggestions at a time
				await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
			}

			/// <summary>
			/// Edit name of existing post!
			/// </summary>
			/// <param name="Id"> Id of the post in database </param>
			/// <param name="newName"> New name to be set! </param>
			/// <returns></returns>
			[HasPermission(PermissionType.ManageStructure)]
            [SlashCommand("edit-name", "Изменить название для уже существующей должности.")]
            public async Task PostEditNameCommand([Summary("posts_autocomplete"), Autocomplete] int Id, string newName) // Praise the code
            {
                var post = _db.Posts.Find(Id);
                var invokerUnit = _db.Units.Find(Context.User.Id);
                if (post != null && await PostHasPermissionToRedactPostAsync(invokerUnit.Posts, post))
                {
                    
                    post.Name = newName;
                    _db.Posts.Update(post);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var err = $"Должность с Id {Id} вызвавший метод не найден в базе данных!";
                    await _logService.WriteAsync(err, LoggingLevel.Warn);
                    await RespondAsync(err, ephemeral: true);
                }
            }

            /// <summary>
            /// Method to edit description of existing post!
            /// </summary>
            /// <param name="Id"> Id of existing post </param>
            /// <param name="newDescription"> New description to be set! </param>
            /// <returns></returns>
            [HasPermission(PermissionType.ManageStructure)]
            [SlashCommand("edit-description", "Изменить описание для уже существующей должности.")]
            public async Task PostEditDescriptionCommand([Summary("posts_autocomplete"), Autocomplete] int Id, string newDescription) // Praise the code
            {
                var post = _db.Posts.Find(Id);
                var invokerUnit = _db.Units.Find(Context.User.Id);
                if (post != null && await PostHasPermissionToRedactPostAsync(invokerUnit.Posts, post))
                {

                    post.Description = newDescription;
                    _db.Posts.Update(post);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var err = $"Должность с Id {Id} вызвавший метод не найден в базе данных!";
                    await _logService.WriteAsync(err, LoggingLevel.Warn);
                    await RespondAsync(err, ephemeral: true);
                }
            }

            /// <summary>
            /// Method to edit permissions of existing post! (will call a select-menu to appear)
            /// </summary>
            /// <param name="Id"> Id of existing post </param>
            /// <returns></returns>
            [HasPermission(PermissionType.ManageStructure)]
            [SlashCommand("edit-permissions", "Изменить разрешения для уже существующей должности. (После вызова появится меню для редактирования)")]
            public async Task PostEditPermissionsCommand([Summary("posts_autocomplete"), Autocomplete] int Id) // Praise the code
            {
                var post = _db.Posts.Find(Id);
                var invokerUnit = _db.Units.Find(Context.User.Id);
                if (post != null && await PostHasPermissionToRedactPostAsync(invokerUnit.Posts, post))
                {

                    var permissionMenuBuilder = new SelectMenuBuilder().WithCustomId($"post-edit-{post.Id}").WithPlaceholder("Разрешения").WithMinValues(0).WithMaxValues(invokerUnit.GetPermissions().Count);
                    foreach (var permission in invokerUnit.GetPermissions())
                    {
                        permissionMenuBuilder.AddOption(label: permission.Type.ToString(), value: ((int)permission.Type).ToString(), description: permission.Description);
                    }
                    var selectMenuSpawner = new ComponentBuilder().WithSelectMenu(permissionMenuBuilder);
                    await RespondAsync("Выберите разрешения доступные этой должности.", components: selectMenuSpawner.Build(), ephemeral: true);
                }
                else
                {
                    var err = $"Должность с Id {Id} вызвавший метод не найден в базе данных!";
                    await _logService.WriteAsync(err, LoggingLevel.Warn);
                    await RespondAsync(err, ephemeral: true);
                }
            }



            [HasPermission(PermissionType.ManageStructure)]
            [SlashCommand("delete", "Удаляет должность по указанному Id")]
            public async Task PostDeleteCommand([Summary("posts_autocomplete"), Autocomplete] int Id) // Praise the code
            {
                var post = _db.Posts.Find(Id);
                var invokerUnit = _db.Units.Find(Context.User.Id);
                if (post != null && (await PostHasPermissionToRedactPostAsync(invokerUnit.Posts, post) || invokerUnit.HasPermission(PermissionType.Administrator)))
                {
                    _db.Posts.Remove(post);
                    await _db.SaveChangesAsync();
                    await RespondAsync("Должность была успешно удалена");
                }
                else
                {
                    var err = $"Должность с Id {Id} вызвавший метод не найден в базе данных!";
                    await _logService.WriteAsync(err, LoggingLevel.Warn);
                    await RespondAsync(err, ephemeral: true);
                }
            }



            /// <summary>
            /// Recursive method to find wether unit is able to redact the post or not.
            /// </summary>
            /// <param name="posts"> Posts from unit.Posts </param>
            /// <param name="postToRedact"> Post that is desired to be changed. </param>
            /// <returns></returns>
            private async Task<bool> PostHasPermissionToRedactPostAsync(List<Post> posts, Post postToRedact) 
            { 
                bool hasPermission = false;
                if (posts.Contains(postToRedact))
                {
                    return true;
                }
                foreach (var post in posts) 
                {
                    if (post.Subordinates.Count > 0) 
                    {
                        if(await PostHasPermissionToRedactPostAsync(post.Subordinates, postToRedact))
                        {
                            hasPermission = true; break; 
                        }
                    }
                }
                return hasPermission;
            }

			[HasPermission(PermissionType.Administrator)]
			[SlashCommand("notify", "Вызвать показ приветственного сообщения")]
			public async Task NotificationShowCommand(int postId, IUser user, [ChannelTypes(ChannelType.Text)] IChannel? channel)
            {
                Unit? unit = await _db.Units.FindAsync(user.Id);
                if (unit == null)
                {
                    await RespondAsync($"Пользователь {user.Username} не найден в системе");
                    await _logService.WriteAsync($"NotificationShowCommand: Пользователь {user.Username} не найден в системе", LoggingLevel.Info);
                    return;
                }

                Post? post = await _db.Posts.FindAsync(postId);
                if (post == null)
				{
					await RespondAsync($"Должность с Id {postId} не найдена в системе");
					await _logService.WriteAsync($"NotificationShowCommand: Должность с Id {postId} не найдена в системе", LoggingLevel.Info);
					return;
				}

                if (post.DiscordNotification == null)
                {
					await RespondAsync($"Должность {post.GetFullName()} не имеет привязанных сообщений");
					await _logService.WriteAsync($"NotificationShowCommand: Должность {post.GetFullName()} не имеет привязанных сообщений", LoggingLevel.Info);
				}
                else
                {
                    await RespondAsync("Сообщение принудительно вызвано.", ephemeral: true);
					await post.NotifyOnAssignAsync(_guildProvider.GetGuild(), _db, unit,
                        channel == null ? post.DiscordNotification.ChannelId : channel.Id);
                }
			}
		}
    }
}
