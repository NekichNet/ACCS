using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Models.Enums;
using Business.Models.SingleDayEvents;
using Business.Models.Util;
using Business.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
namespace Business.Services
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
            string color,
            string conditions,
            string privileges
        )
        {
            ActionResult<Reward> action = new ActionResult<Reward>(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRewards();
                if (!result.IsSuccess)
                    return action.FormFailure("Creating reward restricted. Permission check failed", eventId: EventIds.Forbidden);

                action.Value = new Reward
                {
                    Name = name,
                    Conditions = conditions,
                    Privileges = privileges,
                    Color = color
                };

                await _db.Rewards.AddAsync(action.Value);

				action.Value.UpdateRole();
                await _db.SaveChangesAsync();
				action.FormSuccess($"Reward {action.Value.Name} created", eventId: EventIds.Created);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Reward>> GetAsync(int rewardId)
        {
            ActionResult<Reward> action = new ActionResult<Reward>(_logger);

            try
            {
                action.Value = await _db.Rewards.FindAsync(rewardId);

                if (action.Value != null)
                    action.FormSuccess($"Reward {rewardId} found", eventId: EventIds.Read);
                else
                    action.FormFailure($"Reward with ID {rewardId} not found", eventId: EventIds.NotFound);
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
                action.Value = await _db.Rewards.ToListAsync();

                action.FormSuccess("Reward list formed, length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

		public async Task<ActionResult<List<Reward>>> GetAllActualAsync()
		{
			ActionResult<List<Reward>> action = new ActionResult<List<Reward>>(_logger);

			try
			{
				action.Value = await _db.Rewards.Where(r => r.CanBeAssigned).ToListAsync();

				action.FormSuccess("Reward list formed, length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<EmptyAction> UpdateAsync(int rewardId, string name, string color, string conditions, string privileges)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRewards();
                if (!result.IsSuccess)
                    return action.FormFailure("Updating reward restricted. Permission check failed", eventId: EventIds.Forbidden);

                Reward? reward = await _db.Rewards.FindAsync(rewardId);

                if (reward == null)
                    return action.FormFailure($"Updating reward failed. Reward with ID {rewardId} not found", eventId: EventIds.NotFound);

                reward.Name = name;
                reward.Conditions = conditions;
                reward.Privileges = privileges;
                reward.Color = color;

                _db.Rewards.Update(reward);
                await _db.SaveChangesAsync();

                reward.UpdateRole();

                action.FormSuccess($"Reward {reward.Name} updated", eventId: EventIds.Updated);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<ulong?>> UpdateRoleAsync(int rewardId)
        {
			ActionResult<ulong?> action = new ActionResult<ulong?>(_logger);

            try
            {
                EmptyAction result = await CheckCanManageRewards();
                if (!result.IsSuccess)
                    return action.FormFailure("Updating reward Discord role restricted. Permission check failed", eventId: EventIds.Forbidden);

                Reward? reward = await _db.Rewards.FindAsync(rewardId);

                if (reward == null)
                    return action.FormFailure($"Updating reward Discord role failed. Reward with ID {rewardId} not found", eventId: EventIds.NotFound);

				reward.UpdateRole();
				action.Value = reward.DiscordRoleId;

				await _db.SaveChangesAsync();

				action.FormSuccess($"Reward {reward.Name} Discord role updated", eventId: EventIds.Updated);
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
                Reward? reward = await _db.Rewards.FindAsync(rewardId);
                if (reward == null)
                    return action.FormFailure($"Getting assigned units failed. Reward with ID {rewardId} not found", eventId: EventIds.NotFound);

                var assigned = await _db.AssignedRewards
                    .Where(ar => ar.RewardId == rewardId)
                    .Include(ar => ar.Unit)
                    .ToListAsync();

                action.Value = assigned;
                action.FormSuccess($"Assigned {reward.Name} rewards retrieved, length: " + action.Value.Count,
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<AssignedReward>> AssignAsync(int rewardId, ulong unitId, int? docId = null)
        {
            ActionResult<AssignedReward> action = new ActionResult<AssignedReward>(_logger);

            try
            {
                ActionResult<Reward> result = await CheckCanAssignReward(rewardId);
                if (!result.IsSuccess)
                    return action.FormFailure("Assigning reward restricted. Permission check failed", eventId: EventIds.Forbidden);

                Unit? unit = await _db.Units.FindAsync(unitId);
                if (unit == null)
                    return action.FormFailure($"Assigning reward failed. Unit with ID {unitId} not found", eventId: EventIds.NotFound);

                AssignedReward? existingAssignment = await _db.AssignedRewards
                    .FirstOrDefaultAsync(ar => ar.RewardId == rewardId && ar.UnitId == unit.DiscordId);

                if (existingAssignment != null)
                    return action.FormFailure("Unit already assigned to this reward");

                AssignedReward newAssignedReward = new AssignedReward
                {
                    UnitId = unit.DiscordId,
                    RewardId = rewardId
                };

                RewardAssignmentEvent assignmentEvent = new RewardAssignmentEvent
                {
                    Units = new List<Unit> { unit },
                    AssignedReward = newAssignedReward
                };

                await _db.AssignedRewards.AddAsync(newAssignedReward);
                await _db.RewardAssignmentEvents.AddAsync(assignmentEvent);
                await _db.SaveChangesAsync();

				action.Value = newAssignedReward;

				action.FormSuccess($"Unit {unit.Nickname} assigned to reward {newAssignedReward.Reward.Name}");
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
                AssignedReward? assignedReward = await _db.AssignedRewards
                    .FirstOrDefaultAsync(ar => ar.RewardId == rewardId && ar.UnitId == unitId);

                if (assignedReward == null)
                    return action.FormFailure($"Unit with ID {unitId} not assigned to reward {rewardId}", eventId: EventIds.NotFound);

                action.Value = assignedReward;
                action.FormSuccess($"Unit {assignedReward.Unit.Nickname}'s reward {assignedReward.Reward.Name} assignment retrieved", eventId: EventIds.Read);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }


        public async Task<ActionResult<Reward>> CheckCanAssignReward(int rewardId)
        {
			ActionResult<Reward> action = new ActionResult<Reward>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Permission check failed. Unauthorized", eventId: EventIds.Unauthorized);

				if (!Actor.HasPermission(PermissionType.AssignRewards))
					return action.FormFailure($"Permission check failed. {Actor.Nickname} doesn't have AssignRewards permission", eventId: EventIds.Forbidden);

                action.Value = await _db.Rewards.FindAsync(rewardId);
                if (action.Value == null)
                    return action.FormFailure($"Permission check failed. Reward with ID {rewardId} not found", eventId: EventIds.NotFound);

				action.FormSuccess($"{Actor.Nickname} can assign {action.Value.Name} reward", eventId: EventIds.Accessed);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<EmptyAction> CheckCanManageRewards()
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Permission check failed. Unauthorized", eventId: EventIds.Unauthorized);

				if (!Actor.HasPermission(PermissionType.ManageRewards))
					return action.FormFailure($"Permission check failed. {Actor.Nickname} doesn't have ManageRewards permission", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can manage rewards", eventId: EventIds.Accessed);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
	}
}
