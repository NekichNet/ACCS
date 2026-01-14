using accs.Repository.Interfaces;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class IsUnit : PreconditionAttribute
    {
		public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return (await services.GetRequiredService<IUnitRepository>().ReadAsync(context.User.Id)) == null
                ? PreconditionResult.FromError("Вы не состоите в клане.")
                : PreconditionResult.FromSuccess();
        }
    }
}
