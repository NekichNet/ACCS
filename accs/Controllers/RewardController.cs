using accs.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;

        public RewardController(AppDbContext dbContext, ILogger<UnitController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRewards()
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{rewardId}")]
        public async Task<IActionResult> GetRewardById([FromRoute] int rewardId)
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{rewardId}/discord-role")]
        public async Task<IActionResult> GetRewardDiscordRole([FromRoute] int rewardId)
        {
            try
            {
                var reward = await _dbContext.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId);

                if (reward == null)
                {
                    _logger.LogWarning($"Reward not found: Reward ID {rewardId}");
                    return NotFound(new { error = "Reward not found" });
                }

                if (reward.DiscordRoleId == 0)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = reward.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRewardDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewReward()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReward()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscordRoleReward()
        {
            return await Task.FromResult(Ok());
        }
    }
}
