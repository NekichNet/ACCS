using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reactive;
using System.Xml.Linq;

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
                var post = await _db.Posts.FindAsync(Int32.Parse(postId));
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
        }
    }
}
