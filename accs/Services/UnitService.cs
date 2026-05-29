using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Util;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
	public class UnitService : BusinessService
	{
		private readonly AppDbContext _db;

        public UnitService(AppDbContext db, ILogger logger) : base(logger)
        {
			_db = db;
        }

        public async Task<EmptyAction> RegisterAsync(
			ulong discordId,
			string nickname
			)
		{
			ActionResult<Unit> action = new ActionResult<Unit>(_logger);

			try
			{
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.Administrator))
					{
						action.Value = new Unit
						{
							DiscordId = discordId,
							Nickname = nickname,
							RankUpCounter = 0,
						};



						await _db.Units.AddAsync(action.Value);
						await _db.SaveChangesAsync();

						action.FormSuccess("Unit registered");
					}
					else
					{
						action.FormFailure("Unit registration restricted");
					}
				}
				else
				{
					action.FormFailure("Unit registration restricted. Unauthorized");
				}
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<Unit>> Get(
			ulong discordId
			)
		{
			ActionResult<Unit> action = new ActionResult<Unit>(_logger);

			try
			{
				action.Value = await _db.Units.FindAsync(discordId);
				if (action.Value != null)
					action.FormSuccess("Unit found");
				else
					action.FormFailure("Unit not found");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Unit>>> GetList()
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				action.Value = await _db.Units.ToListAsync();

				action.FormSuccess("Unit list formed. Length: " + action.Value.Count());
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
		
		public async Task<EmptyAction> Dismiss(
			ulong discordId
			)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.DismissUnits))
					{
						Unit? unit = await _db.Units.FindAsync(discordId);
						if (unit != null)
						{
							unit.Posts.Clear();

							action.FormSuccess("Unit dismissed");
						}
						else
						{
							action.FormFailure("Unit not found");
						}
					}
					else
					{
						action.FormFailure("Unit dismissing restricted");
					}
				}
				else
				{
					action.FormFailure("Unit dismissing restricted. Unauthorized");
				}
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
	}
}
