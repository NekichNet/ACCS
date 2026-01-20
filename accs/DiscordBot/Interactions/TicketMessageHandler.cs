using accs.DiscordBot.Preconditions;
using accs.Models;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace accs.DiscordBot.Interactions
{
	[InChannels("TICKET_CHANNEL_ID")]
	public class TicketMessageHandler : InteractionModuleBase<SocketInteractionContext>
	{
		private DiscordSocketClient _client;
		private ILogService _logService;
		private SocketTextChannel _channel;

		public TicketMessageHandler(DiscordSocketClient client, ILogService logService)
		{
			_client = client;
			_logService = logService;

			string channelIdString = DotNetEnv.Env.GetString("TICKET_CHANNEL_ID", "Ticket channel id not found");
			ulong channelId;
			if (ulong.TryParse(channelIdString, out channelId)) { throw _logService.ExceptionAsync("Cannot parse ticket channel id!").Result; }

			_channel = _client.GetChannel(channelId) as SocketTextChannel;
			if (_channel == null) { throw _logService.ExceptionAsync("Ticket channel is null!").Result; }

			_client.Ready += InitTicketMessage;
		}

		[HasPermission(PermissionType.Administrator)]
		[SlashCommand("init-ticket-message", "Инициализация сообщения для тикетов.")]
		public async Task InitTicketMessage()
		{
			await _channel.DeleteMessagesAsync(await _channel.GetMessagesAsync().FlattenAsync());

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Тикеты")
				.WithDescription("Тикет — текстовое обращение, которое видят только Вы и нужная Вам служба.")
				.WithFooter(footer => footer.Text = "Чтобы сформировать тикет, нажмите на соответствующую кнопку ниже:")
				.WithAuthor(Context.Client.CurrentUser)
				.WithColor(Color.DarkGreen);

			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Вступить в клан", "ticket-button-invite", ButtonStyle.Success, new Emoji("U+1FA96"), row: 0)
				.WithButton("Сотрудничество", "ticket-button-friend", ButtonStyle.Primary, new Emoji("U+1F91D"), row: 1)
				.WithButton("Пожертвование", "ticket-button-donation", ButtonStyle.Premium, new Emoji("U+1F4B5"), row: 1)
				.WithButton("Пожаловаться", "ticket-button-report", ButtonStyle.Danger, new Emoji("U+1F91D"), row: 2)
				.WithButton("Техподдержка", "ticket-button-tech", ButtonStyle.Secondary, new Emoji("U+1F91D"), row: 2);

			await _channel.SendMessageAsync(embed: embed.Build());
		}
    }
}
