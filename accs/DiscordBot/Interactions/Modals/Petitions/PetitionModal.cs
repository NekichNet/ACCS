using accs.Database;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions.Modals.Petitions
{
	public abstract class PetitionModal : IModal
	{
		public string Title => "Ходатайство";

		public const string BeginText = "Прошу Вашего ходатайства перед вышестоящим командованием";

		/*
		[ModalMentionableSelect("whom-petition-menu", Placeholder = "Укажите пинги")]
		[InputLabel("Кому", "Убедитесь, что выбираете правильные " +
			"пинги вышестоящих должностей, к которым Вы обращаетесь")]
		public IMentionable[] Whom { get; set; }
		*/

		/*
		[ModalFileUpload("petition-attachments")]
		[InputLabel("Прикреплённые файлы")]
		[RequiredInput(false)]
		public IAttachment[] Attachments { get; set; }
		*/

		public abstract Task<string> GetTextAsync(AppDbContext db);
	}
}
