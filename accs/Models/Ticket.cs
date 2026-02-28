using accs.Database;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text;

namespace accs.Models
{
    public class Ticket
    {
		public int Id { get; set; } = 0;
        public ulong AuthorDiscordId { get; set; }
        public ulong ChannelDiscordId { get; set; }
        public TicketStatus Status { get; set; }
        public ulong? ClosedUserId { get; set; }
		public string? Discriminator { get; set; }

		public Ticket(ulong authorId)
        {
            AuthorDiscordId = authorId;
            Status = TicketStatus.Opened;
        }

        public Ticket() { }

        public virtual async Task AcceptAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
        {
			Status = TicketStatus.Accepted;
			ClosedUserId = closedUserId;
			await DeleteChannelAsync(guildProvider);
            await db.SaveChangesAsync();
        }

        public virtual async Task CancelAsync(IGuildProviderService guildProvider, AppDbContext db)
        {
			Status = TicketStatus.Canceled;
            ClosedUserId = AuthorDiscordId;
			await DeleteChannelAsync(guildProvider);
			await db.SaveChangesAsync();
		}

        public virtual async Task RefuseAsync(IGuildProviderService guildProvider, AppDbContext db, ulong closedUserId)
        {
			Status = TicketStatus.Refused;
			ClosedUserId = closedUserId;
			await DeleteChannelAsync(guildProvider);
			await db.SaveChangesAsync();
		}

        public virtual async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db) { }

        /*
         * Метод, для финального удаления канала тикета с сохранением истории чата.
         */

        public async Task CreateChannelAsync(IGuildProviderService guildProvider, ILogService logService, AppDbContext db)
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
            overwrites.Add(new Overwrite(targetType: PermissionTarget.Role,
                targetId: guild.EveryoneRole.Id, permissions: new OverwritePermissions(viewChannel: PermValue.Deny)));
            overwrites.Add(new Overwrite(targetType: PermissionTarget.User,
                targetId: AuthorDiscordId, permissions: permissions));
            foreach (Post post in GetAdmins(db))
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
			await db.SaveChangesAsync();
		}

        public async Task DeleteChannelAsync(IGuildProviderService guildProvider)
        {
            SocketGuild guild = guildProvider.GetGuild();
			IEnumerable<IMessage> messages = await guild.GetTextChannel(ChannelDiscordId).GetMessagesAsync(100).FlattenAsync();
            string directoryPath = DotNetEnv.Env.GetString("TICKET_MESSAGES_DIRECTORY", "tickets");

			if (!Path.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string dialogue = "";
            string filePath = Path.Join(directoryPath, $"{Id}.txt");

			using (StreamWriter stream = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                foreach (IMessage message in messages.Reverse())
                {
                    DateTime messageTime = message.Timestamp.LocalDateTime;
					string text = $"[{messageTime.ToShortDateString()}, {messageTime.ToShortTimeString()}] " + message.Author.GlobalName + ": " + message.Content + "\n";
					await stream.WriteAsync(text);
                    dialogue += text;
				}
            }
			await guild.GetTextChannel(ChannelDiscordId).DeleteAsync();

            ulong storageChannelId;
            if (ulong.TryParse(DotNetEnv.Env.GetString("TICKET_STORAGE_CHANNEL_ID"), out storageChannelId) == false)
            {
                storageChannelId = guild.PublicUpdatesChannel.Id;
            }

            SocketGuildUser authorUser = guild.GetUser(AuthorDiscordId);
            string authorNickname = "Не найден на сервере";
            if (authorUser != null)
            {
                authorNickname = authorUser.DisplayName;
            }

			SocketGuildUser closedUser = guild.GetUser((ulong)ClosedUserId);
			string closedNickname = "Не найден на сервере";
			if (closedUser != null)
			{
				closedNickname = closedUser.DisplayName;
			}

			SocketTextChannel storageChannel = guild.GetTextChannel(storageChannelId);
            if (storageChannel != null)
                await storageChannel.SendFileAsync(filePath, $"{Discriminator} #{Id} {Status.ToString()}. Автор: {authorNickname}. Закрыл: {closedNickname}");
            else
                Console.WriteLine("Не найден канал для сохранения сообщений тикета!");
		}

        public virtual List<Post> GetAdmins(AppDbContext db)
        {
            List<Post> administrators = db.Posts.ToList().Where(p => p.GetPermissionsRecursive().Any(pr => pr.Type == PermissionType.Administrator)).ToList();
            return administrators;
        }
    }
}
