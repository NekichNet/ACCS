using accs.Database;
using accs.Models.Database;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers.DiscordBot.Interactions
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

        [SlashCommand("table", "Сформировать csv файл всего клана")]
        public async Task ListCommand()
        {
			await DeferAsync();

			await _db.Units.LoadAsync();
			await _db.Ranks.LoadAsync();

			int count = _db.Units.Where(u => u.Posts.Any()).Count();

			await ModifyOriginalResponseAsync(func: (opt) =>
			{
				opt.Content = "Количество бойцов в клане: " + count.ToString()
				+ "\nБойцов в отставке: " + (_db.Units.Count() - count).ToString();
			});

			List<IGrouping<Subdivision?, Post>> posts = (await _db.Posts.FindAsync(1))
				.GetAllSubordinatesRecursive()
				.GroupBy(p => p.Subdivision).ToList();

			if (!Directory.Exists("temp"))
				Directory.CreateDirectory("temp");

			string filePath = Path.Join("temp", "Units.csv");
			using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
			{
				await writer.WriteLineAsync("Должность;Звание;Позывной");
				foreach (IGrouping<Subdivision?, Post> grouping in posts)
				{
					if (grouping.Select(g => g.Units.Count()).Sum() > 0)
						writer.Write("\n");
					foreach (Post post in grouping.ToList())
						foreach (Unit unit in post.Units)
							writer.WriteLine($"{post.GetFullName()};{unit.Rank.Name};{unit.GetOnlyNickname()}");
				}
			}

			await Context.Channel.SendFileAsync(filePath);
		}
    }
}
