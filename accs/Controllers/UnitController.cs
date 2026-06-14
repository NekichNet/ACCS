using accs.Models;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

                var unitsDto = result.Value.Select(u => new
                {
                    Name = u.Nickname,
                    SteamId = u.SteamId.ToString() ?? "",
                    RankUpCounter = u.GetRankUpCounterString(),
                    Joined = u.GetRegistrationDateTimeString(),
                    RankId = u.GetRank()?.Id,
                    PostsIds = u.GetPosts().Select(p => p.Id),
                    AssignedRewardsIds = u.AssignedRewards.Select(ar => ar.Reward.Id).ToList()
                });

                return Ok(unitsDto);
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
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

                if (!ulong.TryParse(dto.DiscordId, out ulong discordId))
                {
                    return BadRequest(new { error = "Передан некорректный формат Discord ID." });
                }

                var result = await _unitService.RegisterAsync(discordId, dto.Nickname);
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
                    Name = result.Value.Nickname,
                    SteamId = result.Value.SteamId.ToString() ?? "",
                    RankUpCounter = result.Value.GetRankUpCounterString(),
                    Joined = result.Value.GetRegistrationDateTimeString(),
                    RankId = result.Value.GetRank()?.Id,
                    PostsIds = result.Value.GetPosts().Select(p => p.Id),
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

        [HttpGet("{discordId}/status")]
        public async Task<IActionResult> GetUnitStatuses([FromRoute] ulong discordId)
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

        [HttpPut("{discordId}/activity")]
        [Authorize]
        public async Task<IActionResult> FixActivity([FromRoute] ulong discordId)
        {
            try
            {
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

                var result = await _unitService.FixActivityAsync(discordId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in FixActivity: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /* Говно галимое. Переделать, пока на сайте не реализовали
		[HttpPut("{discordId}/status")]
		[Authorize]
		public async Task<IActionResult> UpdateUnitStatus([FromRoute] ulong discordId, [FromBody] int statusId)
		{
			try
			{
				_unitService.Actor = HttpContext.Items["Actor"] as Unit;

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

		[HttpPatch("{discordId}")]
		[Authorize]
		public async Task<IActionResult> UpdateUnit([FromRoute] ulong discordId)
		{
			try
			{
				_unitService.Actor = HttpContext.Items["Actor"] as Unit;

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

		[HttpDelete("{discordId}/status/{statusId}")]
        [Authorize]
        public async Task<IActionResult> DeleteStatus([FromRoute] ulong discordId, [FromRoute] int statusId)
        {
            try
            {
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

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
        */

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
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

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
        public string DiscordId { get; set; }
        public string Nickname { get; set; }
        public int? PostsIds { get; set; } = null;
        public int? SubdivisionId { get; set; } = null;
        public int? RankId { get; set; } = null;
        public int? RewardId { get; set; } = null;
    }
}