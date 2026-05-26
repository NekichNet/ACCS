using accs.Database;
using accs.Controllers.DiscordBot.Preconditions;
using accs.Models.Database;
using accs.Models.Enums;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace discord_bot.DiscordBot.Interactions
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminModule> _log;

        public AdminModule(AppDbContext db, ILogger<AdminModule> log)
        {
            _db = db;
            _log = log;
        }

        [SlashCommand("register", "Добавить бойца в систему.")]
        public async Task RegisterUnitCommand(SocketGuildUser user, int postId, int rankId, string? name = null, string? joinedString = null)
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

                DateTime joined = DateTime.UtcNow;
                if (joinedString != null)
                {
					if (!DateTime.TryParse(joinedString, out joined))
                    {
						await RespondAsync($"Не удалось спарсить дату вступления.", ephemeral: true);
						return;
					}
				}

                if (name == null)
                    name = user.DisplayName;

                var unit = new Unit 
                {
                    DiscordId = user.Id,
                    Nickname = name, 
                    Rank = rank, 
                    Posts = new List<Post> { post }, 
                    RankUpCounter = 0,
                    Joined = joined
                };
                
                /*
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
                */

                await _db.Units.AddAsync(unit);
                await _db.SaveChangesAsync();

                await RespondAsync($"Пользователь {name} зарегистрирован на должность {post.GetFullName()} со званием {rank.Name}.", ephemeral: true); 
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Ошибка при создании бойца: {ex.Message}"); 
                await RespondAsync("Произошла ошибка при создании бойца.", ephemeral: true);
            }
        }

        [HasPermission(PermissionType.SteamIdView)]
        [SlashCommand("steam-list", "Высылает csv файл со списком бойцов и их Steam Id.")]
        public async Task GetSteamIdCSVCommand()
        {
            List<Unit> unitsWithSteamid = await _db.Units
                .Where(u => u.Posts.Any())
                .Where(u => u.SteamId != null)
                .ToListAsync();

            int allUsersAmount = _db.Units.Where(u => u.Posts.Any()).Count();
            int usersWithIdAmount = unitsWithSteamid.Count();

			await RespondAsync($"Steam Id привязали {usersWithIdAmount} из {allUsersAmount} бойцов. Высылаю файл...");

			if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
            
            string filePath = Path.Join("temp", "UnitsWithSteamId.csv");
            File.Create(filePath).Close();
            foreach (Unit unit in unitsWithSteamid) 
            {
                await File.AppendAllTextAsync(filePath, $"{unit.Nickname.Replace(",", "")},{unit.SteamId}\n");
            }
            await Context.Channel.SendFileAsync(filePath);
        }
    }
}
