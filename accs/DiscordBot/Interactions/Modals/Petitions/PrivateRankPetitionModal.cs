using accs.Database;
using accs.Models;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions.Modals.Petitions
{
    public class PrivateRankPetitionModal : PetitionModal
	{
		public string Title => "Ходатайство на повышение в звании";

		[ModalUserSelect("user-menu", Placeholder = "Боец на повышение")]
		public IUser UserToRankUp { get; set; }

        public async override Task<string> GetTextAsync(AppDbContext db)
        {
			string unitName = "Ошибка!";
			Unit? unit = await db.Units.FindAsync(UserToRankUp.Id);
			if (unit != null)
			{
				unitName = unit.GetOnlyNickname();
			}

			return $"```ansi\r\n{BeginText} о присвоении воинского звания " +
				$"\u001b[2;32mРядовой\u001b[0m - Рекруту \u001b[2;32m{unitName}\u001b[0m" +
				$" в связи с успешным окочанием курса молодого бойца." +
				$" Дисциплинарных взысканий вышеуказанный служащий не имеет```";
		}
    }
}