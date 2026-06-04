using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Util;
using Microsoft.EntityFrameworkCore;
namespace accs.Services
{
    public class RewardService : BusinessService
    {
        private readonly AppDbContext _db;

        public RewardService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Reward>> CreateAsync(
            string name,
            int? subdivisionId,
            string color,
            string conditions,
            string privileges,
            string? imagePath
        )
        {
            ActionResult<Reward> action = new ActionResult<Reward>(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        action.Value = new Reward
                        {
                            Name = name,
                            Conditions = conditions,
                            Privileges = privileges,
                            Color = color,
                            ImagePath = imagePath
                        };

                        action.Value.UpdateRole();

                        await _db.Rewards.AddAsync(action.Value);
                        await _db.SaveChangesAsync();

                        action.FormSuccess("Reward created");
                    }
                    else
                    {
                        action.FormFailure("Reward creation restricted");
                    }
                }
                else
                {
                    action.FormFailure("Reward creation restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Reward>> GetAsync(int id)
        {
            ActionResult<Reward> action = new ActionResult<Reward>(_logger);

            try
            {
                action.Value = await _db.Rewards
                    .Include(r => r.Assigned)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (action.Value != null)
                    action.FormSuccess("Reward found");
                else
                    action.FormFailure("Reward not found");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<Reward>>> GetAllAsync()
        {
            ActionResult<List<Reward>> action = new ActionResult<List<Reward>>(_logger);

            try
            {
                action.Value = await _db.Rewards
                    .Include(r => r.Assigned)
                    .ToListAsync();
                action.FormSuccess("Reward list formed, length: " + action.Value.Count());
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(int rewardId, string name, string color, string conditions, string privileges, string? imagePath)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var reward = await _db.Rewards.FindAsync(rewardId);
                        if (reward != null)
                        {
                            reward.Name = name;
                            reward.Conditions = conditions;
                            reward.Privileges = privileges;
                            reward.Color = color;
                            if (imagePath != null)
                                reward.ImagePath = imagePath;

                            _db.Rewards.Update(reward);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Reward updated");
                        }
                        else
                        {
                            action.FormFailure("Reward not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Reward update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Reward update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateRoleAsync(int rewardId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var reward = await _db.Rewards.FindAsync(rewardId);
                        if (reward != null)
                        {
                            reward.UpdateRole();
                            _db.Rewards.Update(reward);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Reward discord role updated");
                        }
                        else
                        {
                            action.FormFailure("Reward not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Reward role update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Reward role update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<AssignedReward>>> GetAssignedUnitsAsync(int rewardId)
        {
            ActionResult<List<AssignedReward>> action = new ActionResult<List<AssignedReward>>(_logger);

            try
            {
                var reward = await _db.Rewards.FindAsync(rewardId);
                if (reward == null)
                {
                    action.FormFailure("Reward not found");
                    return action;
                }

                var assignedRewards = await _db.AssignedRewards
                    .Where(ar => ar.RewardId == rewardId)
                    .Include(ar => ar.Unit)
                    .Include(ar => ar.Reward)
                    .ToListAsync();

                action.Value = assignedRewards;
                action.FormSuccess("Assigned rewards retrieved");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<AssignedReward>> AssignAsync(int rewardId, ulong unitId)
        {
            ActionResult<AssignedReward> action = new ActionResult<AssignedReward>(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var unit = await _db.Units.FindAsync(unitId);
                        if (unit == null)
                        {
                            action.FormFailure($"Unit with ID {unitId} not found");
                            return action;
                        }

                        var reward = await _db.Rewards.FindAsync(rewardId);
                        if (reward == null)
                        {
                            action.FormFailure("Reward not found");
                            return action;
                        }

                        var existingAssignment = await _db.AssignedRewards
                            .FirstOrDefaultAsync(ar => ar.RewardId == rewardId && ar.UnitId == unit.DiscordId);

                        if (existingAssignment != null)
                        {
                            action.FormFailure("Unit already assigned to this reward");
                            return action;
                        }

                        var newAssignedReward = new AssignedReward
                        {
                            UnitId = unit.DiscordId,
                            RewardId = rewardId,
                            Display = true
                        };

                        await _db.AssignedRewards.AddAsync(newAssignedReward);
                        await _db.SaveChangesAsync();

                        action.Value = newAssignedReward;
                        action.FormSuccess($"Unit {unit.Nickname} assigned to reward {reward.Name}");
                    }
                    else
                    {
                        action.FormFailure("Reward assignment restricted");
                    }
                }
                else
                {
                    action.FormFailure("Reward assignment restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<AssignedReward>> GetAssignedRewardAsync(int rewardId, ulong unitId)
        {
            ActionResult<AssignedReward> action = new ActionResult<AssignedReward>(_logger);

            try
            {
                var assignedReward = await _db.AssignedRewards
                    .Include(ar => ar.Unit)
                    .Include(ar => ar.Reward)
                    .FirstOrDefaultAsync(ar => ar.RewardId == rewardId && (ulong)ar.UnitId == unitId);

                if (assignedReward == null)
                {
                    action.Value = null;
                    action.FormSuccess($"Unit with ID {unitId} not assigned to reward {rewardId}");
                    return action;
                }

                action.Value = assignedReward;
                action.FormSuccess("Assignment retrieved");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }
    }
}
