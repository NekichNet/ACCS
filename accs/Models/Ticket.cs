using accs.Database;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using System.Text;

namespace accs.Models
{
    public class Ticket
    {
		public int Id { get; set; } = 0;
        public ulong AuthorDiscordId { get; set; }
        public ulong ChannelDiscordId { get; set; }
		public List<Post> Admins { get; set; } = new List<Post>();
        public TicketStatus Status { get; set; }
		public string? Discriminator { get; set; }

		public Ticket(ulong authorId, ulong channelId)
        {
            AuthorDiscordId = authorId;
            ChannelDiscordId = channelId;
            Status = TicketStatus.Opened;
        }

        public Ticket() { }

        public virtual async Task AcceptAsync(IGuildProviderService guildProvider)
        {
			Status = TicketStatus.Accepted;
			await CloseAsync(guildProvider);
        }

        public virtual async Task CancelAsync(IGuildProviderService guildProvider)
        {
			Status = TicketStatus.Canceled;
			await CloseAsync(guildProvider);
		}

        public virtual async Task RefuseAsync(IGuildProviderService guildProvider)
        {
			Status = TicketStatus.Refused;
			await CloseAsync(guildProvider);
		}

        public virtual async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService) { }

        /*
         * Метод, для финального удаления канала тикета с сохранением истории чата.
         */
        public async Task CloseAsync(IGuildProviderService guildProvider)
        {
            SocketGuild guild = guildProvider.GetGuild();
			IEnumerable<IMessage> messages = await guild.GetTextChannel(ChannelDiscordId).GetMessagesAsync(100).FlattenAsync();
            using (FileStream stream = new FileStream(Path.Join(DotNetEnv.Env.GetString("TICKET_MESSAGES_DIRECTORY", "tickets"), $"{Id}.txt"), FileMode.Create))
            {
                foreach (IMessage message in messages)
                {
					byte[] text = Encoding.Unicode.GetBytes($"[{message.Timestamp.ToString()}] " + message.Author.Username + ": " + message.Content + "\n");
					await stream.WriteAsync(text);
				}
            }
			await guild.GetTextChannel(ChannelDiscordId).DeleteAsync();
		}
    }
}
