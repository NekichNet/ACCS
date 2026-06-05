using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.States.Abstraction;
using accs.Models.Statuses;
using accs.Models.Statuses.Abstraction;
using accs.Models.Util;
using accs.Services.Abstraction;
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
                if (Actor == null)
					return action.FormFailure("Unit registration restricted. Unauthorized", eventId: EventIds.Unauthorized);
                if (!Actor.HasPermission(PermissionType.RegisterNewUnits))
                    return action.FormFailure("Unit registration restricted", eventId: EventIds.Forbidden);
                if ((await _db.Units.FindAsync(discordId)) == null)
					return action.FormFailure($"Unit with ID {discordId} already registered", eventId: EventIds.ImpossibleAction);
                
				action.Value = new Unit
				{
					DiscordId = discordId,
					Nickname = nickname
				};

				UnitRegistrationEvent registrationEvent = new UnitRegistrationEvent
				{
					Initiator = Actor,
					Unit = action.Value
				};

				await _db.Units.AddAsync(action.Value);
				await _db.UnitRegistrationEvents.AddAsync(registrationEvent);
				await _db.SaveChangesAsync();

				action.FormSuccess($"Unit {action.Value.Nickname} registered", eventId: EventIds.Created);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<Unit>> GetAsync(ulong discordId)
		{
			ActionResult<Unit> action = new ActionResult<Unit>(_logger);

			try
			{
				action.Value = await _db.Units.FindAsync(discordId);
				if (action.Value != null)
					action.FormSuccess($"Unit {action.Value.Nickname} found", eventId: EventIds.Read);
				else
					action.FormFailure($"Unit with Discord ID {discordId} not found", eventId: EventIds.NotFound);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

        /// <summary>
        /// Получить список абсолютно всех зарегистрированных бойцов
        /// </summary>
		public async Task<ActionResult<List<Unit>>> GetAllAsync()
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

        /// <summary>
        /// Получить всех бойцов, которые на текущий момент в клане
        /// </summary>
        public async Task<ActionResult<List<Unit>>> GetActiveListAsync()
        {
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				action.Value = await _db.Units.Where(u => u.IsActive()).ToListAsync();

				action.FormSuccess("Unit list formed. Length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
		
        /// <summary>
        /// Уволить бойца
        /// </summary>
        /// <param name="discordId">Discord ID бойца к увольнению</param>
		public async Task<EmptyAction> DismissAsync(
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

        /// <summary>
        /// Получить список всех уволенных бойцов
        /// </summary>
        public async Task<ActionResult<List<Unit>>> GetDismissedListAsync()
        {
            ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

            try
            {
                action.Value = await _db.Units
                    .Where(u => !u.IsActive())
                    .Where(u => !u.IsInRetirement())
                    .ToListAsync();

                action.FormSuccess("Dismissed units list formed. Length: " + action.Value.Count(),
                    eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
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
                action.Value = await _db.Units
                    .Where(u => u.IsInRetirement())
                    .ToListAsync();

                action.FormSuccess("Units in retirenment list formed. Length: " + action.Value.Count(),
                    eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<int>>> GetUnitStatusIdsAsync(ulong unitDiscordId)
        {
            ActionResult<List<int>> action = new ActionResult<List<int>>(_logger);

            try
            {
                Unit? unit = await _db.Units.FindAsync(unitDiscordId);
                if (unit == null)
					return action.FormFailure("Getting unit statuses failed. Unit not found", eventId: EventIds.NotFound);

                action.Value = unit.GetStatuses().Select(s => s.Id).ToList();
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
                var unit = await _db.Units
                    .Include(u => u.Activities)
                    .FirstOrDefaultAsync(u => u.DiscordId == discordId);
                if (unit != null)
                {
                    action.Value = unit.Activities
                         .OrderBy(a => a.Date)
                        .Select(ad => ad.Date.ToString("dd.MM.yyyy"))
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
                        var unitStatus = await _db.Statuses.FindAsync(statusId);
                        if (unitStatus != null)
                        {
                            _db.Statuses.Remove(unitStatus);
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

        public async Task<ActionResult<List<int>>> GetPermissionsAsync(ulong discordId)
        {
            ActionResult<List<int>> action = new ActionResult<List<int>>(_logger);

            try
            {
                var unit = await _db.Units
                    .Include(u => u.AssignedRanks).ThenInclude(ar => ar.Rank)
                    .Include(u => u.AssignedPosts).ThenInclude(ap => ap.Post)
                    .FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (unit != null)
                {
                    action.Value = unit.GetPermissions().Select(p => (int)p.Type).ToList();
                    action.FormSuccess("Permissions retrieved");
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

        public async Task<ActionResult<Status>> GetUnitStatusAsync(ulong discordId, int statusId)
        {
            ActionResult<Status> action = new ActionResult<Status>(_logger);

            try
            {
                var unit = await _db.Units
               .Include(u => u.Statuses)
                .FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (unit == null)
                {
                    action.FormFailure("Unit not found");
                }

                var unitStatus = unit.Statuses.FirstOrDefault(us => us.Id == statusId);
                if (unitStatus == null)
                {
                    action.FormFailure("Status not found");
                }

                action.FormSuccess("Status retrieved");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> DeleteAsync(ulong id)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        _logger.LogTrace(EventIds.Processing, $"Searching for unit: {id}");
                        Unit? unit = await _db.Units.FindAsync(id);

                        if (unit != null)
                        {
                            UnitDismissingEvent dismissingEvent = new UnitDismissingEvent()
                            {
                                Initiator = Actor,
                                Unit = unit
                            };
                            _db.UnitDismissingEvents.Add(dismissingEvent);

                            _logger.LogTrace(EventIds.Processing, $"Removing unit from database");
                            _db.Units.Remove(unit);
                            await _db.SaveChangesAsync();

                            action.FormSuccess("Unit deleted", eventId: EventIds.Updated);
                        }
                        else
                        {
                            action.FormFailure("Unit not found", eventId: EventIds.NotFound);
                        }
                    }
                    else
                    {
                        action.FormFailure("Unit deletion restricted", eventId: EventIds.Forbidden);
                    }
                }
                else
                {
                    action.FormFailure("Unit deletion restricted. Unauthorized", eventId: EventIds.Unauthorized);
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
