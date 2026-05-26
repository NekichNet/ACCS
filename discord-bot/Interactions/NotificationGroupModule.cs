using accs.Database;
using accs.Controllers.DiscordBot.Preconditions;
using accs.Models.Database;
using accs.Models.Enums;
using Discord;
using Discord.Interactions;

namespace discord_bot.Interactions
{
	[IsUnit()]
	[Group("notification", "Команды для управления автоматическими сообщениями")]
	public class NotificationGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly AppDbContext _db;

        public NotificationGroupModule(AppDbContext db)
        {
            _db = db;
        }

		[ComponentInteraction("hide:*,*,*", ignoreGroupNames: true)]
		public async Task HideNotificationInteraction(string unitIdString, string notificationIdString, string messageIdString)
		{
			ulong unitId = ulong.Parse(unitIdString);
			int notificationId = int.Parse(notificationIdString);
			DiscordNotification? notification = await _db.DiscordNotifications.FindAsync(notificationId);

			string filePath = Path.Join("temp", $"notification-{messageIdString}.txt");
			string text = "Сообщение не найдено";
			using (StreamReader reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
			{
				text = await reader.ReadToEndAsync();
			}

			Unit? actorUnit = await _db.Units.FindAsync(Context.User.Id);
			if (actorUnit != null)
			{
				if (actorUnit.HasPermission(PermissionType.Administrator))
				{
					File.Delete(filePath);
					await ((IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(messageIdString))).ModifyAsync(m => { m.Embed = null; m.Content = text; });
					await ((IComponentInteraction)Context.Interaction).UpdateAsync(m => m.Components = null);
					await RespondAsync();
					return;
				}
			}

			if (notification == null)
			{
				File.Delete(filePath);
				await ((IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(messageIdString))).ModifyAsync(m => { m.Embed = null; m.Content = text; });
				await ((IComponentInteraction)Context.Interaction).UpdateAsync(m => m.Components = null);
			}
			else if (Context.User.Id == notification.AuthorId || Context.User.Id == unitId)
			{
				File.Delete(filePath);
				await ((IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(messageIdString))).ModifyAsync(m => { m.Embed = null; m.Content = text; });
				await ((IComponentInteraction)Context.Interaction).UpdateAsync(m => m.Components = null);
			}
			else
			{
				await RespondAsync("Вы не можете скрыть это сообщение.", ephemeral: true);
				return;
			}

			await RespondAsync();
		}
    }
}
