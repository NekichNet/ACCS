using accs.Database;
using accs.Models;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions.Modals.Petitions
{
    public class RankUpPetitionModal : PetitionModal
	{
		public string Title => "Ходатайство на повышение в звании";

		[ModalUserSelect("user-menu", Placeholder = "Боец на повышение")]
		public IUser UserToRankUp { get; set; }

		[RequiredInput(false)]
		[ModalSelectMenu("rank-up-petition-menu", Placeholder = "Выберите звание")]
		[ModalSelectMenuOption("Рядовой", "2")]
		[ModalSelectMenuOption("Ефрейтор", "3")]
		[ModalSelectMenuOption("Мл. Сержант", "4")]
		[ModalSelectMenuOption("Сержант", "5")]
		[ModalSelectMenuOption("Ст. Сержант", "6")]
		[ModalSelectMenuOption("Старшина", "7")]
		[ModalSelectMenuOption("Прапорщик", "8")]
		[ModalSelectMenuOption("Ст. Прапорщик", "19")]
		[ModalSelectMenuOption("Мл. Лейтенант", "9")]
		[ModalSelectMenuOption("Лейтенант", "10")]
		[ModalSelectMenuOption("Ст. Лейтенант", "11")]
		[ModalSelectMenuOption("Капитан", "12")]
		[ModalSelectMenuOption("Майор", "13")]
		[ModalSelectMenuOption("Подполковник", "14")]
		[ModalSelectMenuOption("Полковник", "15")]
		[ModalSelectMenuOption("Генерал-Майор", "16")]
		[ModalSelectMenuOption("Генерал-Лейтенант", "17")]
		[ModalSelectMenuOption("Генерал-Полковник", "18")]
		[InputLabel("Новое звание", "Можно оставить пустым. Тогда будет выбрано следующее звание")]
		public string? SelectedRank { get; set; }

		[ModalTextInput("rank-up-petition-reason", minLength: 5)]
		public string Reason { get; set; } = "по выслуге лет";

        public async override Task<string> GetTextAsync(AppDbContext db)
        {
			string isOrdinary = "Ошибка!";
			string rankName = "Ошибка!";
			string unitName = "Ошибка!";
			Unit? unit = await db.Units.FindAsync(UserToRankUp.Id);
			if (unit != null)
			{
				unitName = unit.GetOnlyNickname();
				Rank? rank;
				if (SelectedRank != null)
					rank = await db.Ranks.FindAsync(int.Parse(SelectedRank));
				else
					rank = unit.Rank.Next;
				if (rank != null)
				{
					rankName = rank.Name;
					if (rank.Next != null)
						isOrdinary = rank.Next.Id.ToString() == SelectedRank ? "очередного" : "внеочередного";
				}
			}

			return $"```ansi\r\n{BeginText} о присвоении {isOrdinary} воинского звания " +
				$"\u001b[2;32m{rankName}\u001b[0m - \u001b[2;32m{unitName}\u001b[0m {Reason}." +
				$" Дисциплинарных взысканий вышеуказанный служащий не имеет```";
		}
    }
}