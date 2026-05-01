using accs.Database;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions.Modals.Petitions
{
    public class UniversalPetitionModal : PetitionModal
	{
		[ModalTextDisplay()]
		public string? DisplayBeginText { get; set; } = $"«{BeginText} ...»";

		[ModalTextInput("petition-details", TextInputStyle.Paragraph,
			placeholder: "Например: «о продлении отпуска [Звание] [Никнейм] в " +
			"связи с переездом, длительностью до ДД.ММ.ГГГГ числа включительно»",
			minLength: 10)]
		[InputLabel("Детали ходатайства", "Неправильно оформленное ходатайство могут и не принять!")]
		public string Details { get; set; }

        public async override Task<string> GetTextAsync(AppDbContext db)
        {
            return $"```ansi\r\n{BeginText} {Details}```";
        }
    }
}
