using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.Statuses;
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

						UnitRegistrationEvent registrationEvent = new UnitRegistrationEvent
						{
							Initiator = Actor,
							Unit = action.Value
						};

						await _db.UnitRegistrationEvents.AddAsync(registrationEvent);
						
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

				action.FormSuccess("Unit list formed. Length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
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
						_logger.LogTrace(EventIds.Processing, $"Searching for unit: {discordId}");
						Unit? unit = await _db.Units.FindAsync(discordId);
						
						if (unit != null)
						{
							_logger.LogTrace(EventIds.Read, $"Unit found: {unit.ToString()}");
							foreach (AssignedPost assignedPost in unit.AssignedPosts)
							{
								_logger.LogTrace(EventIds.Processing, $"Termination AssignedPost: {assignedPost.ToString()}");
								assignedPost.Terminate();
								_logger.LogTrace(EventIds.Updated, $"AssignedPost terminated: {assignedPost.ToString()}");
							}

							foreach (AssignedRank assignedRank in unit.AssignedRanks)
							{
								_logger.LogTrace(EventIds.Processing, $"Termination AssignedRank: {assignedRank.ToString()}");
								assignedRank.Terminate();
								_logger.LogTrace(EventIds.Updated, $"AssignedRank terminated: {assignedRank.ToString()}");
							}

							_logger.LogTrace(EventIds.Processing, $"Creating UnitDismissingEvent");
							UnitDismissingEvent dismissingEvent = new UnitDismissingEvent()
							{
								Initiator = Actor,
								Unit = unit
							};
							_db.UnitDismissingEvents.Add(dismissingEvent);
							_logger.LogTrace(EventIds.Created, $"Created UnitDismissingEvent: {dismissingEvent.ToString()}");

							_logger.LogTrace(EventIds.Saving, $"Saving changes");
							await _db.SaveChangesAsync();

							action.FormSuccess("Unit dismissed", eventId: EventIds.Updated);
						}
						else
						{
							action.FormFailure("Unit not found", eventId: EventIds.NotFound);
						}
					}
					else
					{
						action.FormFailure("Unit dismissing restricted", eventId: EventIds.Forbidden);
					}
				}
				else
				{
					action.FormFailure("Unit dismissing restricted. Unauthorized", eventId: EventIds.Unauthorized);
				}
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}


        public async Task<ActionResult<List<Unit>>> GetDismissedUnitsAsync()
        {
            ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

            try
            {
                action.Value = await _db.Units.Where(u => u.Posts.Count == 0).ToListAsync();
                action.FormSuccess("Dismissed units list formed. Length: " + action.Value.Count());
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<Unit>>> GetRetiredUnitsAsync()
        {
            ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

            try
            {
                action.Value = await _db.Units.Where(u => u.IsRetired).ToListAsync();
                action.FormSuccess("Retired units list formed. Length: " + action.Value.Count());
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<int>>> GetUnitStatusesAsync(ulong discordId)
        {
            ActionResult<List<int>> action = new ActionResult<List<int>>(_logger);

            try
            {
                var unit = await _db.Units.FindAsync(discordId);
                if (unit != null)
                {
                    action.Value = unit.UnitStates.Select(us => us.Id).ToList();
                    action.FormSuccess("Unit statuses retrieved");
                }
                else
                {
                    action.FormFailure("Unit not found");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<string>>> GetUnitActivityAsync(ulong discordId)
        {
            ActionResult<List<string>> action = new ActionResult<List<string>>(_logger);

            try
            {
                var unit = await _db.Units.FindAsync(discordId);
                if (unit != null)
                {
                    action.Value = unit.Activities
                        .Select(ad => ad.Date.ToString("dd.MM.yyyy HH:mm"))
                        .ToList();
                    action.FormSuccess("Unit activity retrieved");
                }
                else
                {
                    action.FormFailure("Unit not found");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(ulong discordId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        var unit = await _db.Units.FindAsync(discordId);
                        if (unit != null)
                        {
                            _db.Units.Update(unit);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Unit updated");
                        }
                        else
                        {
                            action.FormFailure("Unit not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Unit update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Unit update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateUnitStatusAsync(ulong discordId, int statusId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        var unit = await _db.Units.FindAsync(discordId);
                        if (unit != null)
                        {
                            _db.Units.Update(unit);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Unit status updated");
                        }
                        else
                        {
                            action.FormFailure("Unit not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Unit status update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Unit status update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateUnitActivityAsync(ulong discordId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        var unit = await _db.Units.FindAsync(discordId);
                        if (unit != null)
                        {
                            _db.Units.Update(unit);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Unit activity updated");
                        }
                        else
                        {
                            action.FormFailure("Unit not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Unit activity update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Unit activity update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> DeleteStatusAsync(int statusId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        var status = await _db.UnitStates.FindAsync(statusId);
                        if (status != null)
                        {
                            _db.UnitStates.Remove(status);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Status deleted");
                        }
                        else
                        {
                            action.FormFailure("Status not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Status deletion restricted");
                    }
                }
                else
                {
                    action.FormFailure("Status deletion restricted. Unauthorized");
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
