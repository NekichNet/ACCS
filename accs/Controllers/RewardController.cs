using accs.Database;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ILogger<RewardController> _logger;

        public RewardController(AppDbContext dbContext, ILogger<RewardController> logger)
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
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateNewReward()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost("{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReward([FromRoute] int rewardId)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost("{rewardId}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdateDiscordRoleReward([FromRoute] int rewardId)
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{rewardId}/assign")]
        [Authorize]
        public async Task<IActionResult> GetAssignedUnits([FromRoute] int rewardId)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost("{rewardId}/assign")]
        [Authorize]
        public async Task<IActionResult> AssignReward([FromRoute] int rewardId)
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{rewardId}/assign/{discordId}")]
        [Authorize]
        public async Task<IActionResult> GetAssignedUnits([FromRoute] int rewardId, [FromRoute] ulong discordId)
        {
            return await Task.FromResult(Ok());
        }
    }
}
