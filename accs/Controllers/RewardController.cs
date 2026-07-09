using accs.Models;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/v1/[controller]")]
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
            try
            {
                var action = await _rewardService.GetAllAsync();
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return StatusCode(500, new { error = "Internal server error" });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllRewards: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rewardId}")]
        public async Task<IActionResult> GetReward([FromRoute] int rewardId)
        {
            try
            {
                var action = await _rewardService.GetAsync(rewardId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Reward not found" });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetReward: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rewardId}/discord-role")]
        public async Task<IActionResult> GetRewardDiscordRole([FromRoute] int rewardId)
        {
            try
            {
                var action = await _rewardService.GetAsync(rewardId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Reward not found" });
                }

                if (action.Value.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = action.Value.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRewardDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReward([FromBody] RewardDto dto)
        {
            try
            {
                _rewardService.Actor = HttpContext.Items["Actor"] as Unit;

                var newReward = await _rewardService.CreateAsync(
                    dto.Name,
                    dto.Color,
                    dto.Conditions,
                    dto.Privileges
                );
                if (!newReward.IsSuccess)
                {
                    return BadRequest(new { error = newReward.Message });
                }

                return Ok(newReward.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateReward: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReward([FromRoute] int rewardId, [FromBody] RewardDto dto)
        {
            try
            {
                _rewardService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _rewardService.UpdateAsync(
                    rewardId,
                    dto.Name,
                    dto.Color,
                    dto.Conditions,
                    dto.Privileges
                );
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateReward: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{rewardId}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdateRewardRole([FromRoute] int rewardId)
        {
            try
            {
                _rewardService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _rewardService.UpdateRoleAsync(rewardId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateRewardRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rewardId}/assign")]
        public async Task<IActionResult> GetAssignedUnits([FromRoute] int rewardId)
        {
            try
            {
                var action = await _rewardService.GetAssignedUnitsAsync(rewardId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Reward not found" });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAssignedUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{rewardId}/assign")]
        [Authorize]
        public async Task<IActionResult> AssignReward([FromRoute] int rewardId, [FromBody] AssignRewardDto dto)
        {
            try
            {
                _rewardService.Actor = HttpContext.Items["Actor"] as Unit;

                if (!ulong.TryParse(dto.DiscordId, out ulong discordId))
                {
                    return BadRequest(new { error = "Передан некорректный формат Discord ID." });
                }

                var action = await _rewardService.AssignAsync(rewardId, discordId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AssignReward: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rewardId}/assign/{discordId}")]
        [Authorize]
        public async Task<IActionResult> GetAssignedUnit([FromRoute] int rewardId, [FromRoute] ulong discordId)
        {
            try
            {
                var action = await _rewardService.GetAssignedRewardAsync(rewardId, discordId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return Ok(null);
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAssignedUnitInfo: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class RewardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Conditions { get; set; } = string.Empty;
        public string Privileges { get; set; } = string.Empty;
        public string Color { get; set; } = "#FFFFFF";
        public string? ImagePath { get; set; }
        public ulong? DiscordRoleId { get; set; }
        public string? DiscordId { get; set; } = string.Empty;
    }

    public class AssignRewardDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "DiscordId field is required.")]
        public string DiscordId { get; set; } = string.Empty;
    }
}
