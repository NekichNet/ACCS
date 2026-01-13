using accs.Models;
using System.Runtime.CompilerServices;

namespace accs.Services.Interfaces
{
    public interface IDiscordBotService
    {
        Task<bool> AddUserRoles(ulong userId, IEnumerable<ulong> roleIds);
		Task<bool> RemoveUserRoles(ulong userId, IEnumerable<ulong> roleIds);
        Task<bool> KickUser(ulong userId);
        Task<bool> BanUser(ulong userId);
	}
}
