using accs.Database;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using static System.Collections.Specialized.BitVector32;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RankController : ControllerBase
    {
        private readonly RankService _rankService;
        private readonly ILogger<RankController> _logger;

        public RankController(RankService rankService, ILogger<RankController> logger)
        {
            _rankService = rankService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRanks()
        {
            try
            {
                var action = await _rankService.GetAllAsync();
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
                _logger.LogError($"Error in GetAllRanks: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rankId}")]
        public async Task<IActionResult> GetRankById([FromRoute] int rankId)
        {
            try
            {
                var action = await _rankService.GetAsync(rankId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Rank not found" });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRankById: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rankId}/permission")]
        public async Task<IActionResult> GetRankPermissions([FromRoute] int rankId)
        {
            try
            {
                var action = await _rankService.GetAsync(rankId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Rank not found" });
                }

                var permissionsIds = action.Value.GetPermissionsRecursive().Select(p => (int)p.Type).ToList();
                return Ok(permissionsIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRankPermissions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{rankId}/discord-role")]
        public async Task<IActionResult> GetRankDiscordRole([FromRoute] int rankId)
        {
            try
            {
                var action = await _rankService.GetAsync(rankId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Rank not found" });
                }

                if (action.Value.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = action.Value.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRankDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateNewRank([FromBody] RankDto dto)
        {
            try
            {
                var newRank = await _rankService.CreateAsync(dto.Id, dto.Name);
                if (!newRank.IsSuccess)
                {
                    return BadRequest(new { error = newRank.Message });
                }
                return Ok(newRank);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateNewRank: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
            
        }

        [HttpDelete("{rankId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRank([FromRoute] int rankId)
        {
            try
            {
                var action = await _rankService.DeleteAsync(rankId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteRank: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{rankId}")]
        [Authorize]
        public async Task<IActionResult> UpdateRank([FromRoute] int rankId, [FromBody] string name)
        {
            try
            {
                var action = await _rankService.UpdateAsync(rankId, name);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateRank: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{rankId}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdateDiscordRoleRank([FromRoute] int rankId)
        {
            try
            {
                var action = await _rankService.UpdateRoleAsync(rankId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateDiscordRoleRank: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class RankDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}