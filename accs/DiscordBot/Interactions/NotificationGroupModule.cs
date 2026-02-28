using accs.Database;
using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.IO;

namespace accs.DiscordBot.Interactions
{
	[IsUnit()]
	[Group("notification", "Команды для управления автоматическими сообщениями")]
	public class NotificationGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly AppDbContext _db;
		private readonly IGuildProviderService _guildProvider;

        public NotificationGroupModule(AppDbContext db, IGuildProviderService guildProvider, ILogService logService)
        {
            _db = db;
            _guildProvider = guildProvider;
        }

		[ComponentInteraction("hide:*,*,*", ignoreGroupNames: true)]
		public async Task HideNotificationInteraction(string unitIdString, string notificationIdString, string messageIdString)
		{
			ulong unitId = ulong.Parse(unitIdString);
			int notificationId = int.Parse(notificationIdString);
			DiscordNotification? notification = await _db.DiscordNotifications.FindAsync(notificationId);

			Unit? actorUnit = await _db.Units.FindAsync(Context.User.Id);
			if (actorUnit != null)
			{
				if (actorUnit.HasPermission(PermissionType.Administrator))
				{
					await ((IComponentInteraction)Context.Interaction).Message.DeleteAsync();
					await (await Context.Channel.GetMessageAsync(ulong.Parse(messageIdString))).DeleteAsync();
					await RespondAsync();
					return;
				}
			}

			if (notification == null)
			{
				await HideMessage(messageIdString);
			}
			else if (Context.User.Id == notification.AuthorId || Context.User.Id == unitId)
			{
				await HideMessage(messageIdString);
			}
			else
			{
				await RespondAsync("Вы не можете скрыть это сообщение.", ephemeral: true);
				return;
			}
			
			await RespondAsync();
		}

		private async Task HideMessage(string messageIdString)
		{
			await (await Context.Channel.GetMessageAsync(ulong.Parse(messageIdString))).DeleteAsync();
			await ((IComponentInteraction)Context.Interaction).Message.DeleteAsync();
		}
    }
}
