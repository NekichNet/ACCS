using accs.Database;
using accs.Models.Enums;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Net;
using System.Text;

namespace accs.Models
{
    public class Ticket
    {
		public int Id { get; set; } = 0;
        public ulong AuthorDiscordId { get; set; }
        public ulong ChannelDiscordId { get; set; }
		public virtual List<Post> Admins { get; set; } = new List<Post>();
        public TicketStatus Status { get; set; }
		public string? Discriminator { get; set; }

		public Ticket(ulong authorId)
        {
            AuthorDiscordId = authorId;
            Status = TicketStatus.Opened;
        }

        public Ticket() { }

        public virtual async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
			Status = TicketStatus.Accepted;
			await DeleteChannelAsync(guildProvider);
        }

        public virtual async Task CancelAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
			Status = TicketStatus.Canceled;
			await DeleteChannelAsync(guildProvider);
		}

        public virtual async Task RefuseAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
			Status = TicketStatus.Refused;
			await DeleteChannelAsync(guildProvider);
		}

        public virtual async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db) { }

        /*
         * Метод, для финального удаления канала тикета с сохранением истории чата.
         */

        public async Task CreateChannelAsync(IGuildProviderService guildProvider, ILogService logService)
        {
			SocketGuild guild = guildProvider.GetGuild();

			ulong categoryId;
            if (!ulong.TryParse(DotNetEnv.Env.GetString("TICKET_CATEGORY_ID", "TICKET_CATEGORY_ID not found"), out categoryId))
            {
                await logService.WriteAsync("Ticket category id is null!", LoggingLevel.Error);
                return;
            };

            OverwritePermissions permissions = new OverwritePermissions(
                addReactions: PermValue.Allow,
                sendMessages: PermValue.Allow,
                attachFiles: PermValue.Allow,
                viewChannel: PermValue.Allow,
                useApplicationCommands: PermValue.Allow
            );

            List<Overwrite> overwrites = new List<Overwrite>();
            overwrites.Add(new Overwrite(targetType: PermissionTarget.User, targetId: AuthorDiscordId, permissions: permissions));
            foreach (Post post in Admins)
                if (post.DiscordRoleId != null)
				    overwrites.Add(new Overwrite(targetType: PermissionTarget.Role, targetId: (ulong)post.DiscordRoleId, permissions: permissions));

			string name = Discriminator != null ? Discriminator + "-" + Id : "Ticket-" + Id;
			
            RestTextChannel channel = await guild.CreateTextChannelAsync(name, x =>
            {
                x.CategoryId = categoryId;
                x.Topic = "Тикет " + guild.GetUser(AuthorDiscordId).Username;
                x.PermissionOverwrites = overwrites;
			});
            ChannelDiscordId = channel.Id;
		}

        public async Task DeleteChannelAsync(IGuildProviderService guildProvider)
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
