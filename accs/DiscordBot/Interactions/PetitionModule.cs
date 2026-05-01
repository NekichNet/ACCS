using accs.Database;
using accs.DiscordBot.Interactions.Modals.Petitions;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Interactions
{
    public class PetitionModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly AppDbContext _db;
		private readonly ILogger _log;

		public PetitionModule(AppDbContext db, ILogger log)
        {
            _db = db;
			_log = log;
        }

		/*
		[HasPermission(PermissionType.WritePetitions)]
		[SlashCommand("petition", "Написать петицию")]
		public async Task PetitionCommand([
			Choice("Повышение", "rank-up"),
			Choice("Повышение до рядового", "private-rank"),
			Choice("Другое", "petition")]
			string petitionType)
		{
			switch (petitionType)
			{
				case "rank-up":
					await RespondWithModalAsync<RankUpPetitionModal>("rank-up-petition-modal");
					break;
				case "private-rank":
					await RespondWithModalAsync<PrivateRankPetitionModal>("private-rank-petition-modal");
					break;
				case "petition":
					await RespondWithModalAsync<PetitionModal>("petition-modal");
					break;
				default:
					await RespondAsync("Ошибка выбора типа ходатайства", ephemeral: true);
					break;
			}
		}
		*/

		[ModalInteraction("*petition-modal")]
		public async Task HandleExampleModal(string petitionType, RankUpPetitionModal modal)
		{
			string unitName = Context.Guild.GetUser(Context.User.Id).DisplayName;
			Unit? unit = await _db.Units.FindAsync(Context.User.Id);
			if (unit != null)
			{
				unitName = unit.GetOnlyNickname();
				if (unit.Posts.Count > 1)
				{
					SelectMenuBuilder menu = new SelectMenuBuilder()
						.WithCustomId("petition-author-post-menu")
						.WithMinValues(1).WithMaxValues(1)
						.WithPlaceholder("Ваша должность");

					foreach (Post post in unit.Posts)
						menu.AddOption(post.Name, post.Id.ToString());

					ComponentBuilder component = new ComponentBuilder()
						.WithSelectMenu(menu);

					await RespondAsync("Выберите одну из своих должностей, " +
						"под которой вы будете писать ходатайство",
						components: component.Build(), ephemeral: true);
				}
				else
				{
					await SendPetitionFormAsync(modal, unit, unit.Posts.First());
					_log.LogInformation("Sent " + petitionType + "-petition by " + unitName);
				}
			}
			else
			{
				await RespondAsync("Ошибка: Вы не найдены в системе!", ephemeral: true);
				_log.LogError($"Пользователь {unitName} с Id {Context.User.Id} не найден в базе данных");
			}
		}

		private async Task SendPetitionFormAsync(PetitionModal modal, Unit author, Post authorPost)
		{
			string mentions = "";
			foreach (Post post in authorPost.GetAllHeadsRecursive())
				if (post.DiscordRoleId != null)
					mentions += Context.Guild.GetRole((ulong)post.DiscordRoleId).Mention;

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle(modal.Title)
				.AddField("", await modal.GetTextAsync(_db))
				.WithColor(Color.DarkRed)
				.AddField("От", author.GetOnlyNickname())
				.AddField("Кому", mentions);

			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Подписать", "sign-petition", ButtonStyle.Success)
				.WithButton("Отказать", "refuse-petition", ButtonStyle.Danger);

			await RespondAsync(mentions, embed: embed.Build(), components: component.Build());
		}
	}
}
