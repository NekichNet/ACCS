using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Models.Enums;
using Business.Models.Statuses;
using Business.Models.Util;
using Business.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class RankService : BusinessService
    {
        private readonly AppDbContext _db;
        private readonly UnitService _unitService;

        public RankService(
            AppDbContext db,
            UnitService unitService,
			ILogger logger)
            : base(logger)
        {
            _db = db;
            _unitService = unitService;
        }

        public async Task<ActionResult<Rank>> CreateAsync(
            string name,
			ushort counterToReach,
			string color,
			int? lowerId
        )
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRanksAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Creating rank restricted. Permission check failed", eventId: EventIds.Forbidden);

                if (lowerId == null)
                    return action.FormFailure("Creating rank failed. Lower rank ID is not provided", eventId: EventIds.InvalidInput);

                Rank? lowerRank = await _db.Ranks.FindAsync(lowerId);
                if (lowerRank == null)
                    return action.FormFailure("Creating rank failed. Lower rank not found", eventId: EventIds.NotFound);

                action.Value = new Rank
                {
                    Name = name,
                    CounterToReach = counterToReach,
                    Color = color,
                    LowerId = lowerId,
                    HigherId = lowerRank.HigherId
                };

                await _db.Ranks.AddAsync(action.Value);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Rank {name} created");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Rank>> GetAsync(int rankId)
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                action.Value = await _db.Ranks.FindAsync(rankId);
                if (action.Value != null)
                    action.FormSuccess("Rank found", eventId: EventIds.Read);
                else
                    action.FormFailure("Rank not found", eventId: EventIds.NotFound);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<Rank>>> GetAllAsync()
        {
            ActionResult<List<Rank>> action = new ActionResult<List<Rank>>(_logger);

            try
            {
                action.Value = await _db.Ranks.ToListAsync();
                action.FormSuccess("Rank list formed, length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> DeleteAsync(int rankId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRanksAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Rank deleting restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure("Rank deleting failed. Rank not found", eventId: EventIds.NotFound);

                if (rank.Higher != null)
                    rank.Higher.LowerId = rank.LowerId;

                if (rank.Lower != null)
                    rank.Lower.HigherId = rank.HigherId;

                _db.Ranks.Remove(rank);

                await _db.SaveChangesAsync();

                action.FormSuccess("Rank deleted");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(
            int rankId,
            string name,
            ushort counterToReach,
            string color,
            int? lowerId
        )
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRanksAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Rank updating restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure("Rank updating failed. Rank not found", eventId: EventIds.NotFound);

                if (lowerId == null && rank.LowerId != null)
                    return action.FormFailure("Rank updating failed. Lower ID is not provided", eventId: EventIds.InvalidInput);

                Rank? lowerRank = await _db.Ranks.FindAsync(lowerId);
                if (lowerRank == null)
                    return action.FormFailure("Rank updating failed. Lower rank not found", eventId: EventIds.NotFound);

                rank.Name = name;
				rank.CounterToReach = counterToReach;
                rank.Color = color;
                rank.LowerId = lowerId;
                rank.HigherId = lowerRank.HigherId;

                rank.UpdateRole();

				_db.Ranks.Update(rank);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Rank {name} updated", eventId: EventIds.Updated);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<ulong?>> UpdateRoleAsync(int rankId)
        {
			ActionResult<ulong?> action = new ActionResult<ulong?>(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRanksAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Updating rank role restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure($"Updating rank role failed. Rank with ID {rankId} not found", eventId: EventIds.NotFound);

				rank.UpdateRole();
                action.Value = rank.DiscordRoleId;

				_db.Ranks.Update(rank);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Rank {rank.Name} with ID {rankId} Discord role updated", eventId: EventIds.Updated);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		/// <summary>
		/// Устанавливает званию разрешения по переданным permission ID.
		/// Перезаписывает только разрешения, выданные конкретно этому званию,
		/// а не унаследованные разрешения от более низких званий.
		/// Попытка снять или установить разрешение, которого нет у пользователя
		/// будет проигнорированна.
		/// </summary>
		public async Task<EmptyAction> UpdatePermissionsAsync(int rankId, List<GivePermissionDto> permissionDtos)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				EmptyAction result = await CheckCanManageRanksAsync();
				if (!result.IsSuccess)
					return action.FormFailure("Updating rank permissions restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure($"Updating rank permissions failed. Rank with ID {rankId} not found", eventId: EventIds.NotFound);

				List<GivedPermission<Rank>> givedPermissions = rank.GivedPermissions.ToList();
				int permissionsHad = givedPermissions.Count;

				foreach (GivedPermission<Rank> givedPermission in givedPermissions)
				{
					if (Actor.HasPermission(givedPermission.PermissionType))
						_db.RankPermissions.Remove(givedPermission);
				}

				foreach (GivePermissionDto permissionDto in permissionDtos)
				{
					if (permissionDto.PermissionId > 0 && permissionDto.PermissionId <= typeof(PermissionType).GetEnumValues().Length)
					{
						PermissionType permissionType = (PermissionType)permissionDto.PermissionId;
						if (Actor.HasPermission(permissionType) && !rank.HasPermission(permissionType))
						{
							Permission? permission = await _db.Permissions.FindAsync(permissionType);
							if (permission != null)
							{
                                _db.RankPermissions.Add(new GivedPermission<Rank>
								{
									Permission = permission,
									Inherit = permissionDto.Inherit,
									EntityId = rankId
								});
							}
						}
					}
				}

                await _db.SaveChangesAsync();

				action.FormSuccess($"Rank {rank.Name} with ID {rankId} permissions updated." +
					$"Then {permissionsHad}, now {rank.GivedPermissions.Count}", eventId: EventIds.Updated);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Unit>>> GetUnitsByRankAsync(int rankId)
        {
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

            try
            {
                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure($"Getting units by rank failed. Rank with ID {rankId} not found", eventId: EventIds.NotFound);

                action.Value = rank.AssignedRanks.Where(r => r.IsActive()).Select(ar => ar.Unit).ToList();

                action.FormSuccess($"Units by {rank.Name} rank with ID {rankId} retrieved",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<AssignedRank>>> AssignMultipleAsync(HashSet<ulong> unitIds, int rankId, int? docId = null)
        {
			ActionResult<List<AssignedRank>> action = new ActionResult<List<AssignedRank>>(_logger);

            try
            {
				if (Actor == null)
					return action.FormFailure("Permission check failed. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignRanks))
					return action.FormFailure($"{Actor.Nickname} don't have AssignRanks permission", eventId: EventIds.Forbidden);

				Rank? rank = await _db.Ranks.FindAsync(rankId);
				if (rank == null)
					return action.FormFailure($"Rank assigning failed. Rank with ID {rankId} not found", eventId: EventIds.NotFound);

				List<AssignedRank> assignedRanks = new List<AssignedRank>();

                foreach (ulong unitId in unitIds)
                {
					ActionResult<Unit> result = await CheckCanChangeRankAsync(unitId);
					if (!result.IsSuccess)
                    {
						_logger.LogWarning(eventId: EventIds.Forbidden,
                            $"Rank assigning to unit with Discord ID {unitId} restricted. Permission check failed");
                        continue;
					}

					AssignedRank? currentAssignment = result.Value.GetAssignedRank();

                    if (currentAssignment == null)
                    {
						_logger.LogError(eventId: EventIds.HandledError,
                            $"Rank assigning to unit with Discord ID {unitId} failed. Can't achieve current assignment");
						continue;
					}

					if (currentAssignment.Rank.Id == rank.Id)
                    {
						_logger.LogInformation(eventId: EventIds.Forbidden, $"Rank assigning failed. Unit {result.Value.Nickname}" +
							$" already assigned to rank {rank.Name} with ID {rankId}");
						continue;
					}

					currentAssignment.Terminate();

					var newAssignedRank = new AssignedRank
					{
						UnitId = result.Value.DiscordId,
						RankId = rankId,
						Start = DateTime.UtcNow
					};

					await _db.AssignedRanks.AddAsync(newAssignedRank);
                    assignedRanks.Add(newAssignedRank);
				}

				action.Value = assignedRanks;

				await _db.SaveChangesAsync();

                action.FormSuccess($"{assignedRanks.Count} units were assigned to rank {rank.Name}", eventId: EventIds.Updated);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		public async Task<ActionResult<List<AssignedRank>>> ChangeMultipleAsync(
            HashSet<ulong> unitIds,
            int steps = 1,
            bool ignorePostMaxRank = false,
            bool isDowngrade = false,
            int? docId = null
            )
		{
			ActionResult<List<AssignedRank>> action = new ActionResult<List<AssignedRank>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Changing rank restricted. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignRanks))
					return action.FormFailure($"Changing rank restricted." +
                        $" {Actor.Nickname} don't have AssignRanks permission", eventId: EventIds.Forbidden);

                if (isDowngrade)
                    steps = -steps;

				List<AssignedRank> assignedRanks = new List<AssignedRank>();

				foreach (ulong unitId in unitIds)
				{
					ActionResult<Unit> result = await CheckCanChangeRankAsync(unitId);
					if (!result.IsSuccess)
					{
						_logger.LogWarning(eventId: EventIds.Forbidden,
                            $"Unit with Discord Id {unitId} Rank changing restricted. Permission check failed");
						continue;
					}

                    AssignedRank? currentAssignment = result.Value.GetAssignedRank();
                    
					if (currentAssignment == null)
					{
						_logger.LogError(eventId: EventIds.HandledError,
							$"Unit {result.Value.Nickname} rank changing failed. Can't get unit's current rank");
						continue;
					}

					Rank currentRank = currentAssignment.Rank;
					Rank? maxRank = result.Value.GetMaxRank();

					if (maxRank == null)
					{
						_logger.LogError(eventId: EventIds.HandledError,
							$"Unit {result.Value.Nickname} rank changing failed. Can't get unit's max rank");
						continue;
					}

					Rank targetRank = currentRank;
                    for (int i = 0; i < Math.Abs(steps); i++)
                    {
                        if (targetRank.Higher != null)
                        {
                            if (ignorePostMaxRank || targetRank.Higher.GetIndex() >= maxRank.GetIndex())
                            {
                                targetRank = targetRank.Higher;
                            }
                        }
                    }

					if (targetRank == currentRank)
                    {
						_logger.LogWarning(eventId: EventIds.ImpossibleAction,
							$"Unit {result.Value.Nickname} rank changing failed. Already achieved rank bounds");
						continue;
					}

					currentAssignment.Terminate();

					var newAssignedRank = new AssignedRank
					{
						Unit = result.Value,
						Rank = targetRank,
						Start = DateTime.UtcNow
					};

					await _db.AssignedRanks.AddAsync(newAssignedRank);
					assignedRanks.Add(newAssignedRank);
				}

                action.Value = assignedRanks;

				await _db.SaveChangesAsync();

				action.FormSuccess($"{assignedRanks.Count} units' ranks were " + 
                    (steps > 0 ? "upgraded" : "downgraded") + $" by {steps} steps",
                    eventId: EventIds.Updated);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<AssignedRank>> GetAssignedRankAsync(int rankId, ulong unitDiscordId)
        {
            ActionResult<AssignedRank> action = new ActionResult<AssignedRank>(_logger);

            try
            {
                AssignedRank? assignedRank = _db.AssignedRanks
                    .AsEnumerable()
                    .FirstOrDefault(
                        ar => ar.UnitId == unitDiscordId
                        && ar.RankId == rankId
                        && ar.IsActive(null));

                if (assignedRank == null)
                    return action.FormFailure($"Unit with Discord ID {unitDiscordId} not assigned to rank {rankId}", eventId: EventIds.NotFound);

				action.Value = assignedRank;
                action.FormSuccess($"Unit's with Discord ID {unitDiscordId} assignment to {assignedRank.Rank.Name} retrieved", eventId: EventIds.Read);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> CheckCanManageRanksAsync()
        {
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't check permissions. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.ManageRanks))
					return action.FormFailure($"{Actor.Nickname} don't have ManageRanks permission", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can manage ranks");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

        public async Task<ActionResult<Unit>> CheckCanChangeRankAsync(ulong unitDiscordId, Unit? unit = null)
        {
            ActionResult<Unit> action = new ActionResult<Unit>(_logger);

            try
            {
                if (Actor == null)
                    return action.FormFailure("Permission check failed. Unauthorized", eventId: EventIds.Unauthorized);
                if (!Actor.HasPermission(PermissionType.AssignRanks))
                    return action.FormFailure($"{Actor.Nickname} don't have AssignRanks permission", eventId: EventIds.Forbidden);

                if (unit == null)
                    unit = await _db.Units.FindAsync(unitDiscordId);
                if (unit == null)
                    return action.FormFailure("Permission check failed. Unit not found", eventId: EventIds.NotFound);
                action.Value = unit;

                if (!unit.IsActive() && !Actor.IsAdmin())
                    return action.FormFailure($"Permission check failed." +
                        $" Unit {unit.Nickname} is in retirement or dismissed", eventId: EventIds.Forbidden);

				HashSet<Post> headPosts = Actor
									.GetPosts()
									.SelectMany(p => p.GetAllHeadsRecursive())
									.ToHashSet();

				if (!Actor.IsAdmin() && _db.Posts.Except(headPosts).Intersect(unit.GetPosts()).Any())
                    return action.FormFailure("Permission check failed. Can't change heads ranks", eventId: EventIds.Forbidden);

                action.FormSuccess($"{Actor.Nickname} can change {action.Value.Nickname}'s rank");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		public async Task<ActionResult<List<Unit>>> GetCanChangeRankUnitsAsync()
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Getting available units to change rank failed. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignRanks))
					return action.FormFailure($"{Actor.Nickname} don't have AssignRanks permission", eventId: EventIds.Forbidden);

                if (Actor.IsAdmin())
                {
                    action.Value = await _db.Units.ToListAsync();
                }
                else
                {
					_unitService.Actor = Actor;
					ActionResult<List<Unit>> result = await _unitService.GetAllNotHeadUnitsAsync();
					if (!result.IsSuccess)
						return action.FormFailure($"Getting available units to change rank failed." +
							$" Handled error in getting all not head units", eventId: EventIds.HandledError);
					action.Value = result.Value.Where(u => u.IsActive()).ToList();
				}

				action.FormSuccess($"{Actor.Nickname}'s available units to change ranks list formed. Length: {action.Value.Count()}",
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