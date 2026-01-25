using accs.Database;
using accs.Models;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

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
			AppDbContext db = services.GetRequiredService<AppDbContext>();
			await db.Permissions.LoadAsync();
			Unit? unit = await db.Units.FindAsync(context.User.Id);
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
