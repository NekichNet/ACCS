using accs.Database;
using accs.Models.Enum;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class IsTicketChannel : PreconditionAttribute
	{
        public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
			return services.GetRequiredService<AppDbContext>().Tickets
                .Where(t => t.Status == TicketStatus.Opened)
                .Where(t => t.ChannelDiscordId == context.Channel.Id)
                .Any()
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Это действие можно сделать только в канале тикета.");
		}
    }
}
