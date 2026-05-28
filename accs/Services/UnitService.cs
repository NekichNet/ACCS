using accs.Models;
using accs.Services.Interfaces;

namespace accs.Services
{
	public class UnitService : BusinessService
	{
		public Models.Action<bool> Register(
			ulong discordId,
			string nickname
			)
		{

		}
		public Models.Action<Unit> Get(
			ulong discordId
			);
		public Models.Action<List<Unit>> GetList(
			int? postId = null,
			int? subdivisionId = null,
			int? rankId = null,
			int? rewardId = null
			);
	}
}
