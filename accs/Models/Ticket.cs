using Discord;
using Discord.WebSocket;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace accs.Models
{
    public class Ticket
    {
        protected SocketGuild _guild;

		public int Id { get; set; } = 0;
        public ulong AuthorDiscordId { get; set; }
        public ulong ChannelDiscordId { get; set; }
		public List<Post> Admins { get; set; } = new List<Post>();
        public TicketStatus Status { get; set; }
		public string? Discriminator { get; set; }

		public Ticket(SocketGuild guild, ulong authorId, ulong channelId)
        {
            _guild = guild;
            AuthorDiscordId = authorId;
            ChannelDiscordId = channelId;
            Status = TicketStatus.Opened;
        }

        public Ticket() { }

        public virtual async Task AcceptAsync()
        {
			Status = TicketStatus.Accepted;
			await CloseAsync();
        }

        public virtual async Task CancelAsync()
        {
			Status = TicketStatus.Canceled;
			await CloseAsync();
		}

        public virtual async Task RefuseAsync()
        {
			Status = TicketStatus.Refused;
			await CloseAsync();
		}

        public virtual async Task SendWelcomeMessageAsync()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null) return;
            await channel.SendMessageAsync();
        }

        /*
         * Метод, для финального удаления канала тикета с сохранением истории чата.
         */
        protected async Task CloseAsync()
        {
            IEnumerable<IMessage> messages = await _guild.GetTextChannel(ChannelDiscordId).GetMessagesAsync(100).FlattenAsync();
            using (FileStream stream = new FileStream(Path.Join(DotNetEnv.Env.GetString("TICKET_MESSAGES_DIRECTORY", "tickets"), $"{Id}.txt"), FileMode.Create))
            {
                foreach (IMessage message in messages)
                {
					byte[] text = Encoding.Unicode.GetBytes(message.Author.Username + ": " + message.Content);
					await stream.WriteAsync(text);
				}
            }
			await _guild.GetTextChannel(ChannelDiscordId).DeleteAsync();
		}
    }
}
