using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Models.Enums;
using Business.Models.SingleDayEvents;
using Business.Models.SingleDayEvents.Abstraction;
using Business.Models.States.Abstraction;
using Business.Models.States.Statuses;
using Business.Models.Statuses;
using Business.Models.Statuses.Abstraction;
using Business.Models.Util;
using Business.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Business.Services
{
	public class UnitService : BusinessService
	{
		private readonly AppDbContext _db;

        public UnitService(AppDbContext db, Logger<UnitService> logger) : base(logger)
        {
			_db = db;
        }

        public async Task<EmptyAction> RegisterAsync(NewUnitDto dto)
		{
			ActionResult<Unit> action = new ActionResult<Unit>(_logger);

			try
			{
                if (Actor == null)
					return action.FormFailure("Unit registration restricted. Unauthorized", eventId: EventIds.Unauthorized);
                if (!Actor.HasPermission(PermissionType.RegisterNewUnits))
                    return action.FormFailure("Unit registration restricted", eventId: EventIds.Forbidden);

				ulong discordId;
				if (!ulong.TryParse(dto.DiscordId, out discordId))
					return action.FormFailure($"Unit registration failed. Incorrect Discord ID", eventId: EventIds.BadData);

				if ((await _db.Units.FindAsync(discordId)) != null)
					return action.FormFailure($"Unit with ID {discordId} already registered", eventId: EventIds.ImpossibleAction);
                
				action.Value = new Unit
				{
					DiscordId = discordId,
					Nickname = dto.Nickname
				};

				UnitRegistrationEvent registrationEvent = new UnitRegistrationEvent
				{
					Initiator = Actor,
                    Units = new List<Unit> { action.Value }
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

		public async Task<ActionResult<Unit>> GetAsync(ulong unitId)
		{
			ActionResult<Unit> action = new ActionResult<Unit>(_logger);

			try
			{
				action.Value = await _db.Units.FindAsync(unitId);
				if (action.Value != null)
					action.FormSuccess($"Unit {action.Value.Nickname} with Discord ID {unitId} found", eventId: EventIds.Read);
				else
					action.FormFailure($"Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);
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
		public async Task<ActionResult<List<Unit>>> GetAllUnitsAsync()
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				action.Value = await _db.Units.ToListAsync();

				action.FormSuccess("All units list formed. Length: " + action.Value.Count(),
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
        public async Task<ActionResult<List<Unit>>> GetActiveUnitsAsync()
        {
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				action.Value = await _db.Units.Where(u => u.IsActive()).ToListAsync();

				action.FormSuccess("Active units list formed. Length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
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
        public async Task<ActionResult<List<Unit>>> GetDismissedUnitsAsync()
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

		/// <summary>
		/// Уволить бойца
		/// </summary>
		public async Task<EmptyAction> DismissAsync(ulong unitId, int? docId = null)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.DismissUnits))
					{
						_logger.LogTrace(EventIds.Processing, $"Searching for unit: {unitId}");
						Unit? unit = await _db.Units.FindAsync(unitId);

						if (unit != null)
						{
							_logger.LogTrace(EventIds.Read, $"Unit found: {unit.ToString()}");
							foreach (AssignedPost assignedPost in unit.GetAssignedPosts())
							{
								_logger.LogTrace(EventIds.Processing, $"Termination AssignedPost: {assignedPost.ToString()}");
								assignedPost.Terminate();
								_logger.LogTrace(EventIds.Updated, $"AssignedPost terminated: {assignedPost.ToString()}");
							}

							foreach (AssignedRank assignedRank in unit.UnitStates.Where(us => us is AssignedRank && us.IsActive()))
							{
								_logger.LogTrace(EventIds.Processing, $"Termination AssignedRank: {assignedRank.ToString()}");
								assignedRank.Terminate();
								_logger.LogTrace(EventIds.Updated, $"AssignedRank terminated: {assignedRank.ToString()}");
							}

							_logger.LogTrace(EventIds.Processing, $"Creating UnitDismissingEvent");
							UnitDismissingEvent dismissingEvent = new UnitDismissingEvent()
							{
								Units = new List<Unit> { unit }
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

		public async Task<ActionResult<List<Status>>> GetUnitStatusesAsync(ulong unitId)
        {
            ActionResult<List<Status>> action = new ActionResult<List<Status>>(_logger);

            try
            {
                Unit? unit = await _db.Units.FindAsync(unitId);
                if (unit == null)
					return action.FormFailure($"Getting unit statuses failed. Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);

                action.Value = unit.GetActiveStatuses();
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<string>>> GetUnitActivityAsync(ulong unitId)
        {
            ActionResult<List<string>> action = new ActionResult<List<string>>(_logger);

            try
            {
                var unit = await _db.Units
                    .Include(u => u.Activities)
                    .FirstOrDefaultAsync(u => u.DiscordId == unitId);
                if (unit != null)
                {
                    action.Value = unit.Activities
                         .OrderBy(a => a.Date)
                        .Select(ad => ad.Date.ToString("dd.MM.yyyy"))
                        .ToList();
                    action.FormSuccess("Unit activity retrieved", eventId: EventIds.Read);
                }
                else
                {
                    action.FormFailure("Unit not found", eventId: EventIds.NotFound);
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		public async Task<EmptyAction> FixActivityAsync(ulong unitId)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Activity fixation restricted. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.FixActivity))
					return action.FormFailure("Activity fixation restricted", eventId: EventIds.Forbidden);

				Unit? unit = await _db.Units.FindAsync(unitId);
				if (unit == null)
					return action.FormFailure($"Activity fixation failed. Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);

				unit.Activities.Add(new Activity
				{
					UnitId = unitId,
					Date = DateOnly.FromDateTime(DateTime.UtcNow)
				});
                unit.RankUpCounter++;

				await _db.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

        public async Task<ActionResult<Status>> AppendStatusAsync(StatusType statusType, ulong unitId, int? docId = null)
        {
            ActionResult<Status> action = new ActionResult<Status>(_logger);

            try
            {
                if (Actor == null)
					return action.FormFailure("Appending status restricted. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignStatuses))
					return action.FormFailure("Appending status restricted", eventId: EventIds.Forbidden);

				Unit? unit = await _db.Units.FindAsync(unitId);
				if (unit == null)
					return action.FormFailure($"Appending status restricted. Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);

                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddDays(7);

			}
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        /* Говно галимое. Переделать, пока на сайте не реализовали.
		public async Task<EmptyAction> UpdateAsync(Unit unit)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.Administrator))
					{
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

		public async Task<EmptyAction> UpdateUnitStatusAsync(ulong unitId, int statusId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.Administrator))
                    {
                        var unit = await _db.Units.FindAsync(unitId);
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
                            action.FormSuccess("Status deleted", eventId: EventIds.Deleted);
                        }
                        else
                        {
                            action.FormFailure("Status not found", eventId: EventIds.NotFound);
                        }
                    }
                    else
                    {
                        action.FormFailure("Status deletion restricted", eventId: EventIds.Forbidden);
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
        */

        public async Task<ActionResult<HashSet<Permission>>> GetPermissionsAsync(ulong unitId)
        {
            ActionResult<HashSet<Permission>> action = new ActionResult<HashSet<Permission>> (_logger);

            try
            {
                Unit? unit = await _db.Units.FindAsync(unitId);

                if (unit != null)
                {
                    action.Value = unit.GetPermissions();
                    action.FormSuccess($"{unit.Nickname}'s permissions retrieved. Length: " + action.Value.Count(),
					    eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
				}
                else
                {
                    action.FormFailure($"Getting unit permissions failed. Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Status>> GetUnitStatusAsync(ulong unitId, int statusId)
        {
            ActionResult<Status> action = new ActionResult<Status>(_logger);

            try
            {
                Unit? unit = await _db.Units.FindAsync(unitId);

                if (unit == null)
                    action.FormFailure($"Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);

                Status? unitStatus = unit.UnitStates
                    .Where(us => us is Status)
                    .Select(us => (Status)us)
                    .FirstOrDefault(us => us.Id == statusId);
                if (unitStatus == null)
                {
                    action.FormFailure($"Status with ID {statusId} not found", eventId: EventIds.NotFound);
                }

                action.FormSuccess("Status retrieved", eventId: EventIds.Read);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<UnitState>>> GetUnitStatesAsync(ulong discordId)
        {
            ActionResult<List<UnitState>> action = new ActionResult<List<UnitState>>(_logger);

            try
            {
                var unit = await _db.Units
                    .FindAsync(discordId);

                if (unit != null)
                {
                    action.Value = unit.UnitStates.OrderByDescending(us => us.Start).ToList();
                    action.FormSuccess($"States history for unit {discordId} formed. Length: " + action.Value.Count,
                        eventId: action.Value.Count > 0 ? EventIds.Read : EventIds.NoData);
                }
                else
                {
                    action.FormFailure($"Unit with Discord ID {discordId} not found", eventId: EventIds.NotFound);
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<SingleDayEvent>>> GetUnitEventsAsync(ulong unitId)
        {
            ActionResult<List<SingleDayEvent>> action = new ActionResult<List<SingleDayEvent>>(_logger);

            try
            {
                var unit = await _db.Units
                    .FindAsync(unitId);

                if (unit != null)
                {
                    action.Value = unit.SingleDayEvents.OrderByDescending(e => e.DateTime).ToList();
                    action.FormSuccess($"Events history for unit {unitId} formed. Length: " + action.Value.Count,
                        eventId: action.Value.Count > 0 ? EventIds.Read : EventIds.NoData);
                }
                else
                {
                    action.FormFailure($"Unit with Discord ID {unitId} not found", eventId: EventIds.NotFound);
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		public async Task<ActionResult<List<FavoriteKit>>> GetAvailableKits()
		{
			ActionResult<List<FavoriteKit>> action = new ActionResult<List<FavoriteKit>>(_logger);

			try
			{
                if (Actor == null)
                    return action.FormFailure("Getting available kits failed. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = await _db.FavoriteKits.ToListAsync();

				action.FormSuccess($"Available kits for unit {Actor.Nickname} with Discord ID {Actor.DiscordId} formed. Length: " + action.Value.Count,
						eventId: action.Value.Count > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<BackgroundPicture>>> GetAvailableBackgrounds()
		{
			ActionResult<List<BackgroundPicture>> action = new ActionResult<List<BackgroundPicture>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Getting available backgrounds failed. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = await _db.BackgroundPictures.ToListAsync();

				action.FormSuccess($"Available backgrounds for unit {Actor.Nickname} with Discord ID {Actor.DiscordId} formed. Length: " + action.Value.Count,
						eventId: action.Value.Count > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Unit>>> GetAllNotHeadUnitsAsync()
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all not head units. Unauthorized", eventId: EventIds.Unauthorized);

				HashSet<Unit> headUnits = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllHeadsRecursive())
					.SelectMany(p => p.AssignedPosts.Select(ap => ap.Unit))
					.ToHashSet();

				action.Value = await _db.Units.Except(headUnits).ToListAsync();

				action.FormSuccess($"{Actor.Nickname}'s all not head units retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<HashSet<Unit>>> GetAllHeadUnitsAsync()
		{
			ActionResult<HashSet<Unit>> action = new ActionResult<HashSet<Unit>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all head units. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllHeadsRecursive())
					.SelectMany(p => p.AssignedPosts.Select(ap => ap.Unit))
					.ToHashSet();

				action.FormSuccess($"{Actor.Nickname}'s all head units retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<HashSet<Unit>>> GetAllSubordinateUnitsAsync()
		{
			ActionResult<HashSet<Unit>> action = new ActionResult<HashSet<Unit>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all subordinate units. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllSubordinatesRecursive())
					.SelectMany(p => p.AssignedPosts.Select(ap => ap.Unit))
					.ToHashSet();

				action.FormSuccess($"{Actor.Nickname}'s all subordinate units retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
	}
}
