using accs.Repository.Interfaces;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class IsUnit : PreconditionAttribute
    {
        private bool _isUnit;

        public IsUnit(bool isUnit = true)
        {
            _isUnit = isUnit;
        }

		public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
			return ((await services.GetRequiredService<IUnitRepository>().ReadAsync(context.User.Id)) == null) == _isUnit
				? PreconditionResult.FromError((_isUnit ? "Вы не" : "Вы") + " состоите в клане.")
				: PreconditionResult.FromSuccess();
		}
    }
}
