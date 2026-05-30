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
        AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;
        UnitService _unitService;

        public UnitController(UnitService unitService, ILogger<UnitController> logger, AppDbContext dbContext)
        {
            _unitService = unitService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits([FromBody] UnitDto dto)
        {
            try
            {
                var result = await _unitService.GetList();
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                var units = result.Value?.Select(u => new
                {
                    u.Nickname,
                    SteamId = u.SteamId.ToString() ?? "",
                    RankUpCounter = $"{u.RankUpCounter}/15",
                    Joined = u.RegistrationEvent.DateTime.ToString("dd.MM.yyyy HH:mm") ?? "",
                    RankId = u.AssignedRanks,
                    PostsIds = u.AssignedPosts,
                    AssignedRewardsIds = u.AssignedRewards.Select(ar => ar.Reward.Id).ToList()
                }).ToList();

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
                var result = await _unitService.Get(id);
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
                    Joined = result.Value.Joined.ToString("dd.MM.yyyy HH:mm"),
                    RankId = result.Value.Rank?.Id,
                    PostsIds = result.Value.Posts.Select(p => p.Id).ToList(),
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


        [HttpPatch("{id}")]
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
                var result = await _unitService.GetStatusesAsync(discordId);
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
                var result = await _unitService.GetStatusesAsync(discordId);
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
                var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {discordId}");
                    return NotFound(new { error = "Unit not found" });
                }

                var permissions = unit.GetPermissions().Select(p => (int)p.Type).ToList();

                return Ok(permissions);
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
                var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {discordId}");
                    return NotFound(new { error = "Unit not found" });
                }

                var unitStatus = unit.UnitStates.FirstOrDefault(us => us.Id == statusId);

                if (unitStatus == null)
                {
                    _logger.LogWarning($"Status not found: Status ID {statusId}");
                    return NotFound(new { error = "Status not found" });
                }

                var result = new
                {
                    UnitId = unitStatus.Unit.DiscordId.ToString(),
                    Status = new
                    {
                        unitStatus.Status.Name,
                        Color = unitStatus.Status.DiscordRoleId?.ToString() ?? ""
                    },
                    Start = unitStatus.Start.ToString("dd.MM.yyyy HH:mm"),
                    End = unitStatus.End?.ToString("dd.MM.yyyy HH:mm") ?? ""
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitStatus: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUnitStatus([FromRoute] ulong id)
        {
            return await Task.FromResult(Ok());
        }


        [HttpPut]
        [Authorize]
        public async Task<IActionResult> FixUnitActivity()
        {
            return await Task.FromResult(Ok()); // это можно в сервисе реализовать
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteStatus()
        {
            return await Task.FromResult(Ok());
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