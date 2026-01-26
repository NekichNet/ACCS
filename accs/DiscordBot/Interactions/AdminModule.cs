using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    [HasPermission(PermissionType.Administrator)]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly IGuildProviderService _guildProvider;
        private readonly ILogService _logService;

        public AdminModule(AppDbContext db, IGuildProviderService guildProvider, ILogService logService)
        {
            _db = db;
            _guildProvider = guildProvider;
            _logService = logService;
        }

        [SlashCommand("register", "Добавить бойца")]
        public async Task RegisterUnitCommand(IUser user, int postId, int rankId)
        {
            try
            {
                var existingUnit = await _db.Units.FindAsync(user.Id);
                if (existingUnit != null)
                {
                    await RespondAsync($"Боец с Discord ID {user.Id} уже существует.", ephemeral: true);
                    return;
                }

                var post = await _db.Posts.FindAsync(postId); 
                if (post == null) 
                {
                    await RespondAsync( $"Должность с ID: {postId} не найдена.", ephemeral: true ); 
                    return; 
                }

                var rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null) 
                {
                    await RespondAsync($"Звание с ID: {rankId} не найдено.", ephemeral: true);
                    return; 
                }

                var unit = new Models.Unit 
                { 
                    DiscordId = user.Id, 
                    Nickname = user.Username, 
                    Rank = rank, 
                    Posts = new List<Post> { post }, 
                    RankUpCounter = 0 
                };
                
                var newUser = _guildProvider.GetGuild().GetUser(user.Id);

                if (rankId == 1)
                {
                    await newUser.ModifyAsync(x => 
                    { 
                        x.Nickname = $"[Р] {user.Username}"; 
                    });
                }
                else 
                {
                    await newUser.ModifyAsync(x =>
                    {
                        x.Nickname = $"[РХБЗ] {user.Username}";
                    });
                }

                await _db.Units.AddAsync(unit);
                await _db.SaveChangesAsync();

                await RespondAsync($"Пользователь {user.Username} зарегистрирован на должность {post.GetFullName()} со званием {rank.Name}.", ephemeral: true); 
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка при создании бойца: {ex.Message}", LoggingLevel.Error); 
                await RespondAsync("Произошла ошибка при создании бойца.", ephemeral: true);
            }
        }
    }
}
