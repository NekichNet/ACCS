using accs.DiscordBot.Preconditions;
using accs.Repository;
using accs.Repository.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Channels;

namespace accs.Models.Tickets
{
    public class InviteTicket : Ticket
    {
        private readonly IPostRepository _postRepository; 
        private readonly IUnitRepository _unitRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IRankRepository _rankRepository;

        public InviteTicket
            (
             SocketGuild guild,
             ulong authorId,
             ulong channelId,
             IPostRepository postRepository,
             IUnitRepository unitRepository,
             ITicketRepository ticketRepository,
             IRankRepository rankRepository
            ) : base(guild, authorId, channelId)
        {
            _postRepository = postRepository;
            _unitRepository = unitRepository;
            _ticketRepository = ticketRepository;
            _rankRepository = rankRepository;
        }


        public async Task SendWelcomeMessage()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            await channel.SendMessageAsync(
                "Добро пожаловать! Оставьте заявку на вступление в клан. " +
                "Военная полиция скоро свяжется с вами."
            );
        }


        public override async Task Accept()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);

            var allPosts = await _postRepository.ReadAllAsync();
            var shooterPosts = allPosts
                .Where(p => p.Name == "Стрелок")
                .ToList();

            if (!shooterPosts.Any())
            {
                await channel.SendMessageAsync("Ошибка: не найдено ни одной должности 'Стрелок'.");
                return;
            }

            var menu = new SelectMenuBuilder() 
                .WithCustomId($"invite-select-{Id}")
                .WithPlaceholder("Выберите взвод для рекрута")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (Post post in shooterPosts)
                menu.AddOption(post.GetFullName(), post.Id.ToString());

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await channel.SendMessageAsync(
                "Выберите взвод, за которым будет закреплён новичок:",
                components: builder.Build()
            );
        }


        public async Task FinalAcceptance(int selectedPostId)
        {

            var channel = _guild.GetTextChannel(ChannelDiscordId);          

            // назначаем должность стрелка
            var post = await _postRepository.ReadAsync(selectedPostId);

            if (post == null)
            {
                await channel.SendMessageAsync("Ошибка: выбранная должность не найдена.");
                return;
            }

            // выдаём звание рекрута
            var recruitRank = await _rankRepository.ReadAsync(1);

            var unit = new Unit
            {
                DiscordId = AuthorDiscordId,
                Nickname = "[Р] " + _guild.GetUser(AuthorDiscordId).DisplayName,
                Rank = recruitRank,
                Posts = new List<Post> { post }
            };

            await _unitRepository.CreateAsync(unit);

            Status = TicketStatus.Accepted;
            await Close();
        }
    }
}
