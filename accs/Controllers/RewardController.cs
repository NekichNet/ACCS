using accs.Database;
using accs.Services;
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
        private readonly RewardService _rewardService;
        private readonly ILogger<RewardController> _logger;

        public RewardController(RewardService rewardService, ILogger<RewardController> logger)
        {
            _rewardService = rewardService;
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

        [HttpPatch("{rewardId}")]
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

    public class RewardDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int? SubdivisionId { get; set; }
        public ulong? DiscordRoleId { get; set; }
    }
}
