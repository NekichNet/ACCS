using accs.Repository.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class RetirementTicket : Ticket
    {
        private readonly IUnitRepository _unitRepository;
        private readonly IPostRepository _postRepository;
        public RetirementTicket(SocketGuild guild, ulong authorId, ulong channelId, 
            IUnitRepository unitRepository, IPostRepository postRepository
            ) : base(guild, authorId, channelId)
        {
            _unitRepository = unitRepository;
            _postRepository = postRepository;
        }

        public override async Task SendWelcomeMessage()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;
            await channel.SendMessageAsync(
                "Вы подали заявку на выход в отставку или вернуться из неё.\n" +
                "Командир РХБЗ или его заместитель скоро рассмотрят ваш запрос."
            );
        }


        public override async Task Accept()
        {
            var unit = await _unitRepository.ReadAsync(AuthorDiscordId);
            var channel = _guild.GetTextChannel(ChannelDiscordId);

            if (unit == null)
            {
                await channel.SendMessageAsync("Ошибка: боец не найден в базе.");
                return;
            }

            // Если боец не в отставке
            if (unit.Status != UnitStatus.Retirement)
            {
                unit.Posts.Clear(); // снимаем все должности
                unit.Status = UnitStatus.Retirement;

                await _unitRepository.UpdateAsync(unit);

                await channel.SendMessageAsync(
                    "Вы успешно отправлены в отставку. Все ваши должности сняты."
                );

                Status = TicketStatus.Accepted;
                await Close();
                return;
            }

            // Если боец уже в отставке
            var allPosts = await _postRepository.ReadAllAsync();

            var menu = new SelectMenuBuilder()
                .WithCustomId($"retirement-select-{Id}")
                .WithPlaceholder("Выберите новые должности")
                .WithMinValues(0)
                .WithMaxValues(allPosts.Count);

            foreach (var post in allPosts)
                menu.AddOption(post.GetFullName(), post.Id.ToString());

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await channel.SendMessageAsync(
                "Выберите должности, которые хотите получить после возвращения из отставки:",
                components: builder.Build()
            );
        }


        public async Task ReturnFromRetirementHandler(List<int> postIds)
        {
            var unit = await _unitRepository.ReadAsync(AuthorDiscordId);
            var channel = _guild.GetTextChannel(ChannelDiscordId);

            if (unit == null)
            {
                await channel.SendMessageAsync("Ошибка: боец не найден.");
                return;
            }

            // Снимаем статус Retirement
            unit.Status = UnitStatus.Active;

            // Очищаем должности
            unit.Posts.Clear();

            // Назначаем выбранные должности
            foreach (var id in postIds)
            {
                var post = await _postRepository.ReadAsync(id);
                if (post != null)
                    unit.Posts.Add(post);
            }

            await _unitRepository.UpdateAsync(unit);

            await channel.SendMessageAsync("Вы успешно вернулись из отставки.");

            Status = TicketStatus.Accepted;
            await Close();
        }

    }
}
