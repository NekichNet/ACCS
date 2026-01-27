using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogService _logService;

        public AdminModule(AppDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        [SlashCommand("register", "Добавить бойца в систему.")]
        public async Task RegisterUnitCommand(SocketGuildUser user, int postId, int rankId, string? name = null, DateTime? joined = null)
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

                if (joined == null)
                    joined = DateTime.UtcNow;
                if (name == null)
                    name = user.DisplayName;

                var unit = new Unit 
                {
                    DiscordId = user.Id,
                    Nickname = name, 
                    Rank = rank, 
                    Posts = new List<Post> { post }, 
                    RankUpCounter = 0,
                    Joined = (DateTime)joined
                };
                
                if (rankId == 1)
                {
                    await user.ModifyAsync(x => 
                    { 
                        x.Nickname = $"[Р] {name}"; 
                    });
                }
                else 
                {
                    await user.ModifyAsync(x =>
                    {
                        x.Nickname = $"[РХБЗ] {name}";
                    });
                }

                await _db.Units.AddAsync(unit);
                await _db.SaveChangesAsync();

                await RespondAsync($"Пользователь {name} зарегистрирован на должность {post.GetFullName()} со званием {rank.Name}.", ephemeral: true); 
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync($"Ошибка при создании бойца: {ex.Message}", LoggingLevel.Error); 
                await RespondAsync("Произошла ошибка при создании бойца.", ephemeral: true);
            }
        }
    }
}
