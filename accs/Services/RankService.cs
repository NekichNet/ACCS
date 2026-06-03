
using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Statuses;
using accs.Models.Util;
using Microsoft.EntityFrameworkCore;
using System;

namespace accs.Services
{
    public class RankService : BusinessService
    {
        private readonly AppDbContext _db;
        public RankService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Rank>> CreateAsync(int id, string name)
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        action.Value = new Rank(id, name);

                        await _db.Ranks.AddAsync(action.Value);
                        await _db.SaveChangesAsync();

                        action.FormSuccess("Rank created");
                    }
                    else
                    {
                        action.FormFailure("Rank creation restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank creation restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Rank>> GetAsync(int id)
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                action.Value = await _db.Ranks.FindAsync(id);
                if (action.Value != null)
                    action.FormSuccess("Rank found");
                else
                    action.FormFailure("Rank not found");
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
                action.FormSuccess("Rank list formed, length: " + action.Value.Count());
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
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var rank = await _db.Ranks.FindAsync(rankId);
                        if (rank != null)
                        {
                            _db.Ranks.Remove(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank deleted");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank deletion restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank deletion restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(int rankId, string name)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var rank = await _db.Ranks.FindAsync(rankId);
                        if (rank != null)
                        {
                            rank.Name = name;
                            _db.Ranks.Update(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank updated");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateRoleAsync(int rankId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var rank = await _db.Ranks.FindAsync(rankId);
                        if (rank != null)
                        {
                            rank.UpdateRole();
                            _db.Ranks.Update(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank discord role updated");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank role update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank role update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> GetUnitsByRankAsync(int rankId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                var rank = await _db.Ranks.FindAsync(rankId);
                if (rank == null)
                {
                    action.FormFailure("Rank not found");
                    return action;
                }

                var assignedRanks = await _db.AssignedRanks
                    .Where(ar => ar.RankId == rankId)
                    .Include(ar => ar.Unit)
                    .ToListAsync();

                action.FormSuccess("Units by rank retrieved");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> AssignAsync(int rankId, ulong id)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.AssignRanks))
                    {
                        Unit unit = await _db.Units.FindAsync(id);

                        var rank = await _db.Ranks.FindAsync(rankId);
                        if (rank == null)
                        {
                            action.FormFailure("Rank not found");
                            return action;
                        }

                        var res = await _db.Units
                            .Include(u => u.AssignedRanks)
                            .FirstOrDefaultAsync(u => u.DiscordId == unit.DiscordId);

                        if (res == null)
                        {
                            action.FormFailure("Unit not found");
                            return action;
                        }

                        var activeRank = res.AssignedRanks.FirstOrDefault(ar => ar.IsActive());
                        if (activeRank != null)
                        {
                            activeRank.End = DateTime.UtcNow;
                            _db.AssignedRanks.Update(activeRank);
                        }

                        var newAssignedRank = new AssignedRank
                        {
                            UnitId = res.DiscordId,
                            RankId = rankId,
                            Start = DateTime.UtcNow,
                            End = null
                        };

                        await _db.AssignedRanks.AddAsync(newAssignedRank);
                        await _db.SaveChangesAsync();

                        action.FormSuccess("Rank assigned to res");
                    }
                    else
                    {
                        action.FormFailure("Rank assignment restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank assignment restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<AssignedRank>> GetAssignedUnitAsync(int rankId, ulong unitDiscordId)
        {
            ActionResult<AssignedRank> action = new ActionResult<AssignedRank>(_logger);

            try
            {
                Unit? unit = await _db.Units
                    .FirstOrDefaultAsync(u => u.DiscordId == unitDiscordId);

                if (unit == null)
                {
                    action.FormFailure("Unit not found");
                    return action;
                }

                var assignedRank = unit.AssignedRanks
                    .FirstOrDefault(ar => ar.RankId == rankId && ar.IsActive());

                if (assignedRank == null)
                {
                    action.Value = null;
                    action.FormSuccess("No active assignment");
                    return action;
                }

                action.Value = assignedRank;
                action.FormSuccess("Assignment retrieved");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Unit>> CheckCanChangeRankAsync(ulong unitDiscordId, Unit? unit)
        {
            ActionResult<Unit> action = new ActionResult<Unit>(_logger);

            try
            {
                if (Actor == null)


                if (unit == null)
                    unit = await _db.Units.FindAsync(unitDiscordId);
                if (unit == null)
                    return action.FormFailure("Permission checking failed. Unit not found", eventId: EventIds.NotFound);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }
    }
}