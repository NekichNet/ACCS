using accs.Models;
using accs.Repository.Interfaces;
using Discord;
using Discord.Interactions;

namespace accs.DiscordBot.Preconditions
{
    public class HasPermission : PreconditionAttribute
    {
        private readonly PermissionType _permissionType;

        public HasPermission(PermissionType permissionType)
        {
            _permissionType = permissionType;
        }

        public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
			Console.WriteLine("Huh4");
			Unit? unit = await services.GetRequiredService<IUnitRepository>().ReadAsync(context.User.Id);
			if (unit != null)
			{
				if (unit.HasPermission(_permissionType))
					return PreconditionResult.FromSuccess();
				else
					return PreconditionResult.FromError("У вас нет разрешения на это действие.");
			}
			else
				return PreconditionResult.FromError("Вы не найдены в базе данных бойцов.");
		}
    }
}
