using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class InChannels : PreconditionAttribute
	{
        private List<ulong> _ids = new List<ulong>();

        public InChannels(params string[] ids)
        {
            foreach (string idString in ids)
            {
				ulong id;
                if (ulong.TryParse(DotNetEnv.Env.GetString(idString, $"{idString} not found"), out id))
                    _ids.Add(id);
                else
                    Console.WriteLine($"Error: {idString} not found");
			}
		}

        public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return _ids.Contains(context.Channel.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Это действие нельзя совершить в этом канале.");
        }
    }
}
