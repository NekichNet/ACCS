using accs.Database;
using accs.Models;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        private readonly ILogger<UnitController> _logger;
        UnitService _unitService;

        public UnitController(UnitService unitService, ILogger<UnitController> logger)
        {
            _unitService = unitService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            try
            {
                var result = await _unitService.GetAllAsync();
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                var units = result.Value;

                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnits: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RegisterUnit([FromBody] UnitDto dto)
        {
            try
            {
                var result = await _unitService.RegisterAsync(dto.DiscordId, dto.Nickname);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RegisterUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnit([FromRoute] ulong id)
        {
            try
            {
                var result = await _unitService.GetAsync(id);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {id}");
                    return NotFound(new { error = "Unit not found" });
                }

                var unitResult = new
                {
                    result.Value.Nickname,
                    SteamId = result.Value.SteamId.ToString() ?? "",
                    RankUpCounter = $"{result.Value.RankUpCounter}/15",
                    Joined = result.Value.RegistrationEvent.DateTime.ToString("dd.MM.yyyy HH:mm"),
                    RankId = result.Value.AssignedRanks.Select(r => r.Id),
                    PostsIds = result.Value.AssignedPosts.Select(p => p.Id).ToList(),
                    AssignedRewardsIds = result.Value.AssignedRewards.Select(ar => ar.Reward.Id).ToList()
                };

                return Ok(unitResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPatch("{discordId}")]
        public async Task<IActionResult> UpdateUnit([FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.UpdateAsync(discordId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{discordId}/status")]
        public async Task<IActionResult> GetUnitStatuses( [FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.GetUnitStatusIdsAsync(discordId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {discordId}");
                    return NotFound(new { error = "Unit not found" });
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitStatuses: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{discordId}/activity")]
        public async Task<IActionResult> GetUnitActivity([FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.GetUnitActivityAsync(discordId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {discordId}");
                    return NotFound(new { error = "Unit not found" });
                }
                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitActivity: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{discordId}/permission")]
        public async Task<IActionResult> GetUnitPermissions([FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.GetPermissionsAsync(discordId);

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitPermissions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{discordId}/status/{statusId}")]
        public async Task<IActionResult> GetUnitStatus([FromRoute] ulong discordId, [FromRoute] int statusId)
        {
            try
            {
                // проблема со статусом
                var result = await _unitService.GetUnitStatusAsync(discordId, statusId);

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitStatus: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPut("{discordId}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateUnitStatus([FromRoute] ulong discordId, [FromBody] UnitStatusDto dto)
        {
            try
            {
                var result = await _unitService.UpdateUnitStatusAsync(discordId, dto.StatusId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateUnitStatus: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPut("{discordId}/activity")]
        [Authorize]
        public async Task<IActionResult> UpdateUnitActivity([FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.UpdateUnitActivityAsync(discordId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateUnitActivity: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{discordId}/status/{statusId}")]
        [Authorize]
        public async Task<IActionResult> DeleteStatus([FromRoute] ulong discordId, [FromRoute] int statusId)
        {
            try
            {
                var result = await _unitService.DeleteStatusAsync(statusId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteStatus: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dismissed")]
        public async Task<IActionResult> GetDismissedUnits()
        {
            try
            {
                var result = await _unitService.GetDismissedListAsync();
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                var units = result.Value;

                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDismissedUnits: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("retirement")]
        public async Task<IActionResult> GetRetiredUnits()
        {
            try
            {
                var result = await _unitService.GetRetiredUnitsAsync();
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                var units = result.Value;

                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRetiredUnits: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUnit([FromRoute] ulong id)
        {
            try
            {
                var result = await _unitService.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class UnitDto
    {
        public ulong DiscordId { get; set; }
        public string Nickname { get; set; }
        public int? Post { get; set; } = null;
        public int? Subdivision { get; set; } = null;
        public int? Rank { get; set; } = null;
        public int? Reward { get; set; } = null;
    }

    public class UnitStatusDto
    {
        public int StatusId { get; set; }
    }
}