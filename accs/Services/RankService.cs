using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Statuses;
using accs.Models.Util;
using accs.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class RankService : BusinessService
    {
        private readonly AppDbContext _db;
        public RankService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
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
                EmptyAction result = await CheckCanManageAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Creating rank restricted. Permission check failed", eventId: EventIds.Forbidden);

                if (lowerId == null)
                    return action.FormFailure("Creating rank failed. Lower rank ID is not provided", eventId: EventIds.InvalidData);

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
                EmptyAction result = await CheckCanManageAsync();
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
                EmptyAction result = await CheckCanManageAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Rank updating restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure("Rank updating failed. Rank not found", eventId: EventIds.NotFound);

                if (lowerId == null && rank.LowerId != null)
                    return action.FormFailure("Rank updating failed. Lower ID is not provided", eventId: EventIds.InvalidData);

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
                EmptyAction result = await CheckCanManageAsync();
                if (!result.IsSuccess)
                    return action.FormFailure("Updating rank role restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure("Updating rank role failed. Rank not found", eventId: EventIds.NotFound);

				rank.UpdateRole();
                action.Value = rank.DiscordRoleId;

				_db.Ranks.Update(rank);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Rank {rank.Name} Discord role updated", eventId: EventIds.Updated);
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
                    return action.FormFailure("Getting units by rank failed. Rank not found", eventId: EventIds.NotFound);

                action.Value = rank.AssignedRanks.Where(r => r.IsActive()).Select(ar => ar.Unit).ToList();

                action.FormSuccess($"Units by {rank.Name} rank retrieved",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<AssignedRank>> AssignAsync(int rankId, ulong unitId)
        {
			ActionResult<AssignedRank> action = new ActionResult<AssignedRank>(_logger);

            try
            {
                ActionResult<Unit> result = await CheckCanChangeRankAsync(unitId);
                if (!result.IsSuccess)
                    return action.FormFailure("Rank assigning restricted. Permission check failed", eventId: EventIds.Forbidden);

                Rank? rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                    return action.FormFailure("Rank assigning failed. Rank not found", eventId: EventIds.NotFound);

                AssignedRank currentAssignment = result.Value.GetAssignedRank();

                if (currentAssignment.Rank.Id == rank.Id)
                    return action.FormFailure($"Rank assigning failed. Unit {result.Value.Nickname} already assigned to this rank", eventId: EventIds.ImpossibleAction);

                currentAssignment.Terminate();

                var newAssignedRank = new AssignedRank
                {
                    UnitId = result.Value.DiscordId,
                    RankId = rankId
                };

                await _db.AssignedRanks.AddAsync(newAssignedRank);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Unit {result.Value.Nickname} assigned to rank {rank.Name}", eventId: EventIds.Created);
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
                AssignedRank? assignedRank = _db.AssignedRanks.FirstOrDefault(ar => ar.UnitId == unitDiscordId && ar.RankId == rankId && ar.IsActive(null));

                if (assignedRank == null)
                    return action.FormFailure($"Unit with Discord ID {unitDiscordId} not assigned to rank {rankId}", eventId: EventIds.NotFound);

				action.Value = assignedRank;
                action.FormSuccess($"{assignedRank.Unit.Nickname} assignment to {assignedRank.Rank.Name} retrieved", eventId: EventIds.Read);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> CheckCanManageAsync()
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

                if (!unit.IsActive())
                    return action.FormFailure("Permission check failed. Unit is in retirement or dismissed", eventId: EventIds.Forbidden);

                if (Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).Intersect(unit.GetPosts()).Any())
                    return action.FormFailure("Permission check failed. Can't change heads ranks", eventId: EventIds.Forbidden);

                action.FormSuccess($"{Actor.Nickname} can change {action.Value.Nickname}'s rank");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }
    }
}