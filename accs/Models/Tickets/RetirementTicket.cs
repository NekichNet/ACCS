using accs.Database;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Tickets
{
    public class RetirementTicket : Ticket
    {
        private readonly AppDbContext _db;

        public RetirementTicket(AppDbContext db, ulong authorId, ulong channelId) : base(authorId, channelId)
        {
            _db = db;
        }

        public override async Task SendWelcomeMessageAsync(IGuildProviderService guildProvider, ILogService logService)
        {
			SocketTextChannel channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);
			if (channel == null)
				await logService.WriteAsync("RetirementTicket: channel is null");
			else
				await channel.SendMessageAsync(
                "Вы подали заявку на выход в отставку или вернуться из неё.\n" +
                "Командир РХБЗ или его заместитель скоро рассмотрят ваш запрос."
            );
        }


        public override async Task AcceptAsync(IGuildProviderService guildProvider)
        {
            await _db.UnitStatuses.LoadAsync();
            await _db.Posts.LoadAsync();

            Unit? unit = await _db.Units.FindAsync(AuthorDiscordId);
            var channel = guildProvider.GetGuild().GetTextChannel(ChannelDiscordId);

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

                Status? retirementStatus = await _db.Statuses.FindAsync(StatusType.Retirement);
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

                await _db.UnitStatuses.AddAsync(unitStatus);

                await channel.SendMessageAsync(
                    "Вы успешно отправлены в отставку. Все ваши должности сняты."
                );

                Status = TicketStatus.Accepted;
                await CloseAsync(guildProvider);
                return;
            }

            // уже в отставке -> показываем меню выбора должностей
            List<Post> allPosts = await _db.Posts.Include(p => p.Units).ToListAsync();

            var menu = new SelectMenuBuilder()
                .WithCustomId($"retirement-select-{Id}")
                .WithPlaceholder("Должности")
                .WithMinValues(1)
                .WithMaxValues(allPosts.Count);

            foreach (Post post in allPosts)
            {
                menu.AddOption(post.GetFullName(), post.Id.ToString(), post.Units.Count + " человек");
            }

            var builder = new ComponentBuilder().WithSelectMenu(menu);

            await channel.SendMessageAsync(
                "Выберите должности, которые хотите получить после возвращения из отставки:",
                components: builder.Build()
            );
        }
    }
}
