using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class InChannels : PreconditionAttribute
	{
        private List<ulong> _ids = new List<ulong>();

        public InChannels(params string[] ids)
        {
            foreach (string id in ids)
                _ids.Add(ulong.Parse(DotNetEnv.Env.GetString(id, $"{id} Not found")));
		}

        public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return _ids.Contains(context.Channel.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Это действие нельзя совершить в этом канале.");
        }
    }
}
