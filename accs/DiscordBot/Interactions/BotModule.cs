using accs.Database;
using accs.Models;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.DiscordBot.Interactions
{
    public class BotModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly AppDbContext _db;

		public BotModule(AppDbContext db)
		{
			_db = db;
		}

        [SlashCommand("ping", "Проверить, работает ли бот")]
        public async Task PingCommand()
        {
            await RespondAsync("Ok");
        }

        [SlashCommand("table", "Сформировать таблицу со всеми бойцами клана")]
        public async Task ListCommand()
        {
			await DeferAsync();

			_db.Units.Load();
			_db.Ranks.Load();
			_db.Posts.Load();

			List<Unit> units = (await _db.Units
				.ToListAsync())
				.OrderByDescending(u => u.Rank.Id)
				.GroupBy(u => u.Posts.Any() ? u.Posts.First().SubdivisionId : 999)
				.SelectMany(g => g.ToList().GroupBy(u => u.Posts.FirstOrDefault()).SelectMany(sg => sg.ToList()))
				.ToList();

			await ModifyOriginalResponseAsync(func: (opt) =>
			{
				opt.Content = "Количество бойцов в клане: " + units.Count().ToString();
			});

			if (!Directory.Exists("temp"))
				Directory.CreateDirectory("temp");

			string filePath = Path.Join("temp", "Units.csv");
			File.Create(filePath).Close();
			await File.AppendAllTextAsync(filePath, $"Должность,Звание,Позывной\r\n");
			foreach (Unit unit in units)
			{
				string postName = unit.Posts.Any() ? unit.Posts.First().GetFullName() : "Отставка";
				await File.AppendAllTextAsync(filePath, $"{postName},{unit.Rank.Name},{unit.GetOnlyNickname().Replace(",", "")}\r\n");
			}
			await Context.Channel.SendFileAsync(filePath);
		}
    }
}
