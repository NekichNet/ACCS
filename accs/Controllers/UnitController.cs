using accs.Models;
using accs.Models.SingleDayEvents.Abstraction;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        private readonly UnitService _unitService;
        private readonly ILogger<UnitController> _logger;

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
                    DiscordId = u.DiscordId,
                    Nickname = u.Nickname,
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

        [HttpGet("{unitId}")]
        public async Task<IActionResult> GetUnit([FromRoute] ulong unitId)
        {
            try
            {
                var result = await _unitService.GetAsync(unitId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {unitId}");
                    return NotFound(new { error = "Unit not found" });
                }

                var unitResult = new
                {
                    DiscordId = result.Value.DiscordId,
                    Nickname = result.Value.Nickname,
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

        [HttpGet("{unitId}/status")]
        public async Task<IActionResult> GetUnitStatuses([FromRoute] ulong unitId)
        {
            try
            {
                var result = await _unitService.GetUnitStatusIdsAsync(unitId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {unitId}");
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

        [HttpGet("{unitId}/activity")]
        public async Task<IActionResult> GetUnitActivity([FromRoute] ulong unitId)
        {
            try
            {
                var result = await _unitService.GetUnitActivityAsync(unitId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {unitId}");
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

        [HttpGet("{unitId}/permission")]
        public async Task<IActionResult> GetUnitPermissions([FromRoute] ulong discordId)
        {
            try
            {
                var result = await _unitService.GetPermissionIdsAsync(discordId);

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

        [HttpGet("{unitId}/status/{statusId}")]
        public async Task<IActionResult> GetUnitStatus([FromRoute] ulong unitId, [FromRoute] int statusId)
        {
            try
            {
                var result = await _unitService.GetUnitStatusAsync(unitId, statusId);

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

        [HttpPut("{unitId}/activity")]
        [Authorize]
        public async Task<IActionResult> FixActivity([FromRoute] ulong unitId)
        {
            try
            {
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

                var result = await _unitService.FixActivityAsync(unitId);
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
		[HttpPut("{unitId}/status")]
		[Authorize]
		public async Task<IActionResult> UpdateUnitStatus([FromRoute] ulong unitId, [FromBody] int statusId)
		{
			try
			{
				_unitService.Actor = HttpContext.Items["Actor"] as Unit;

				var result = await _unitService.UpdateUnitStatusAsync(unitId, dto.StatusId);
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

		[HttpPatch("{unitId}")]
		[Authorize]
		public async Task<IActionResult> UpdateUnit([FromRoute] ulong unitId)
		{
			try
			{
				_unitService.Actor = HttpContext.Items["Actor"] as Unit;

				var result = await _unitService.UpdateAsync(unitId);
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

		[HttpDelete("{unitId}/status/{statusId}")]
        [Authorize]
        public async Task<IActionResult> DeleteStatus([FromRoute] ulong unitId, [FromRoute] int statusId)
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

        [HttpDelete("{unitId}")]
        [Authorize]
        public async Task<IActionResult> DismissUnit([FromRoute] ulong unitId)
        {
            try
            {
                _unitService.Actor = HttpContext.Items["Actor"] as Unit;

                var result = await _unitService.DismissAsync(unitId);
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

        [HttpGet("{unitId}/states")]
        public async Task<IActionResult> GetUnitStates([FromRoute] ulong unitId)
        {
            try
            {

                var result = await _unitService.GetUnitStatesAsync(unitId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    return NotFound(new { error = "Unit not found" });
                }

				List<StateDto> dtos = result.Value.Select(e => new StateDto
				{
					Id = e.Id,
					Text = e.GetText(),
					Color = e.GetHexColor(),
					Start = e.Start,
                    End = e.End,
					UnitId = unitId
				}).ToList();

				return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitState: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{unitId}/events")]
        public async Task<IActionResult> GetSingleDayEvents([FromRoute] ulong unitId)
        {
            try
            {
                var result = await _unitService.GetUnitEventsAsync(unitId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                if (result.Value == null)
                {
                    return NotFound(new { error = "Unit not found" });
                }

                List<EventDto> dtos = result.Value.Select(e => new EventDto {
                    Id = e.Id,
                    Text = e.GetText(),
                    Color = e.GetHexColor(),
                    DateTime = e.DateTime,
                    UnitId = unitId
                }).ToList();

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSingleDayEvent: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class StateDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
		public string Color { get; set; }
		public DateTime Start { get; set; }
		public DateTime? End { get; set; }
		public ulong UnitId { get; set; }
	}

	public class EventDto
	{
		public int Id { get; set; }
		public string Text { get; set; }
        public string Color { get; set; }
		public DateTime DateTime { get; set; }
		public ulong UnitId { get; set; }
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