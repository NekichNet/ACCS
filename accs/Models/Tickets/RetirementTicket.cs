using accs.Repository.Interfaces;
using Discord;
using Discord.WebSocket;

namespace accs.Models.Tickets
{
    public class RetirementTicket : Ticket
    {
        private readonly IUnitRepository _unitRepository;
        private readonly IPostRepository _postRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IUnitStatusRepository _unitStatusRepository;

        public RetirementTicket(SocketGuild guild, ulong authorId, ulong channelId, 
               IUnitRepository unitRepository, IPostRepository postRepository, 
               IStatusRepository statusRepository, IUnitStatusRepository unitStatusRepository
            ) : base(guild, authorId, channelId)
        {
            _unitRepository = unitRepository;
            _postRepository = postRepository;
            _statusRepository = statusRepository;
            _unitStatusRepository = unitStatusRepository;
        }

        public override async Task SendWelcomeMessageAsync()
        {
            var channel = _guild.GetTextChannel(ChannelDiscordId);
            if (channel == null)
                return;
            await channel.SendMessageAsync(
                "Вы подали заявку на выход в отставку или вернуться из неё.\n" +
                "Командир РХБЗ или его заместитель скоро рассмотрят ваш запрос."
            );
        }


        public override async Task AcceptAsync()
        {
            var unit = await _unitRepository.ReadAsync(AuthorDiscordId);
            var channel = _guild.GetTextChannel(ChannelDiscordId);

            if (unit == null)
            {
                await channel.SendMessageAsync("Ошибка: боец не найден.");
                return;
            }

            // поиск активного статуса Retirement
            var activeRetirement = unit.UnitStatuses
                .FirstOrDefault(us =>
                    us.Status.Type == StatusType.Retirement &&
                    us.EndDate > DateTime.UtcNow
                );

            // еще не в отставке -> в отставку
            if (activeRetirement == null)
            {
                unit.Posts.Clear();
                await _unitRepository.UpdateAsync(unit);

                var retirementStatus = await _statusRepository.ReadAsync(StatusType.Retirement);
                if (retirementStatus == null)
                {
                    await channel.SendMessageAsync("Ошибка: статус Retirement не найден.");
                    return;
                }

                var unitStatus = new UnitStatus
                {
                    Unit = unit,
                    Status = retirementStatus,
                    StartDate = DateTime.UtcNow
                };

                await _unitStatusRepository.CreateAsync(unitStatus);

                await channel.SendMessageAsync(
                    "Вы успешно отправлены в отставку. Все ваши должности сняты."
                );

                Status = TicketStatus.Accepted;
                await CloseAsync();
                return;
            }

            // уже в отставке -> показываем меню выбора должностей
            var allPosts = await _postRepository.ReadAllAsync();

            var menu = new SelectMenuBuilder()
                .WithCustomId($"retirement-select-{Id}")
                .WithPlaceholder("Должности")
                .WithMinValues(1)
                .WithMaxValues(allPosts.Count);

            foreach (var post in allPosts)
            {
                menu.AddOption(post.GetFullName(), post.Id.ToString(), post.Units.Count + " человек");
            }

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

            // поиск активного статуса Retirement
            var activeRetirement = unit.UnitStatuses
                .FirstOrDefault(us =>
                    us.Status.Type == StatusType.Retirement &&
					us.EndDate > DateTime.UtcNow
				);

            if (activeRetirement != null)
            {
                activeRetirement.EndDate = DateTime.UtcNow;
                await _unitStatusRepository.UpdateAsync(activeRetirement);
            }

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
            await CloseAsync();
        }
    }
}
