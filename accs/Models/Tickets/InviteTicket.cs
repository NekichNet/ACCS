using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class InviteTicket : Ticket
    {
        private readonly ILogService _logService;

        public InviteTicket
            (
             SocketGuild guild,
             ulong authorId,
             ulong channelId,
             ILogService logService
            ) : base(guild, authorId, channelId)
        {
            _logService = logService;
        }


        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService)
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            await channel.SendMessageAsync(
                "Добро пожаловать! Оставьте заявку на вступление в клан. " +
                "Военная полиция скоро свяжется с вами."
            );
        }


        public override async Task AcceptAsync()
        {
            SocketTextChannel channel = _guild.GetTextChannel(ChannelDiscordId);

            List<Post> shooterPosts = (await _postRepository.ReadAllAsync())
				.Where(p => p.Name == "Стрелок")
                .ToList();

            if (!shooterPosts.Any())
            {
                await channel.SendMessageAsync("Ошибка: не найдено ни одной должности 'Стрелок'.");
                return;
            }

            var menu = new SelectMenuBuilder() 
                .WithCustomId($"invite-select-{Id}")
                .WithPlaceholder("Взвод")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (Post post in shooterPosts)
                menu.AddOption(post.GetFullName(), post.Id.ToString(), post.Units.Count + " человек");

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await channel.SendMessageAsync(
                "Выберите взвод, за которым будет закреплён рекрут:",
                components: builder.Build()
            );
        }


        public async Task AcceptanceHandler(int selectedPostId)
        {

            var channel = _guild.GetTextChannel(ChannelDiscordId);          

            // назначаем должность стрелка
            var post = await _postRepository.ReadAsync(selectedPostId);

            if (post == null)
            {
                await channel.SendMessageAsync($"Ошибка: выбранная должность стрелка с Id {selectedPostId} не найдена!");
                await _logService.WriteAsync($"Ошибка: выбранная должность стрелка с Id {selectedPostId} не найдена!", LoggingLevel.Error);
                return;
            }

            // выдаём звание рекрута
            var recruitRank = await _rankRepository.ReadAsync(1);

			if (recruitRank == null)
			{
				await channel.SendMessageAsync("Ошибка: звание рекрута не найдено.");
				return;
			}

            await _guild.GetUser(AuthorDiscordId).ModifyAsync(u => u.Nickname = "[Р] " + u.Nickname);

			var unit = new Unit
            {
                DiscordId = AuthorDiscordId,
                Nickname = _guild.GetUser(AuthorDiscordId).DisplayName,
                Rank = recruitRank,
                Posts = new List<Post> { post }
            };

            await _unitRepository.CreateAsync(unit);

            Status = TicketStatus.Accepted;
            await CloseAsync();
        }
    }
}
