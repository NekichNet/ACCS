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

        public virtual async Task Accept()
        {
			Status = TicketStatus.Accepted;
			await Close();
        }

        public virtual async Task Cancel()
        {
			Status = TicketStatus.Canceled;
			await Close();
		}

        public virtual async Task Refuse()
        {
			Status = TicketStatus.Refused;
			await Close();
		}

        /*
         * Метод, для финального удаления канала тикета с сохранением истории чата.
         */
        protected async Task Close()
        {
            IEnumerable<IMessage> messages = await _guild.GetTextChannel(ChannelDiscordId).GetMessagesAsync(100).FlattenAsync();
            using (FileStream stream = new FileStream(DotNetEnv.Env.GetString("TICKET_MESSAGES_DIRECTORY", Path.Join("messages", $"{Id}.txt")), FileMode.Create))
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
