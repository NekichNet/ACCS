using accs.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;

        public UnitController(AppDbContext dbContext, ILogger<UnitController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            try
            {
                var query = _dbContext.Units.AsQueryable();

                if (post.HasValue)
                {
                    query = query.Where(u => u.Posts.Any(p => p.Id == post.Value));
                }

                if (subdivision.HasValue)
                {
                    query = query.Where(u => u.Posts.Any(p => p.Subdivision.Id == subdivision.Value));
                }

                if (rank.HasValue)
                {
                    query = query.Where(u => u.Rank.Id == rank.Value);
                }

                if (reward.HasValue)
                {
                    query = query.Where(u => u.AssignedRewards.Any(ar => ar.Reward.Id == reward.Value));
                }

                var units = await query.Select(u => new
                {
                    u.Nickname,
                    SteamId = u.SteamId.ToString() ?? "",
                    RankUpCounter = $"{u.RankUpCounter}/15",
                    Joined = u.Joined.ToString("dd.MM.yyyy HH:mm"),
                    u.Rank.Id,
                    PostsIds = u.Posts.Select(p => p.Id).ToList(),
                    AssignedRewardsIds = u.AssignedRewards.Select(ar => ar.Reward.Id).ToList()
                }).ToListAsync();

                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnits: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegisterUnit()
        {
            return await Task.FromResult(Ok());
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnit(
            [FromRoute] ulong id)
        {
            try
            {
                var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == id);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {id}");
                    return NotFound(new { error = "Unit not found" });
                }

                var result = new
                {
                    unit.Nickname,
                    SteamId = unit.SteamId.ToString() ?? "",
                    RankUpCounter = $"{unit.RankUpCounter}/15",
                    Joined = unit.Joined.ToString("dd.MM.yyyy HH:mm"),
                    RankId = unit.Rank.Id,
                    PostsIds = unit.Posts.Select(p => p.Id).ToList(),
                    AssignedRewardsIds = unit.AssignedRewards.Select(ar => ar.Reward.Id).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUnit(
            [FromRoute] ulong discordId)
        {
            return await Task.FromResult(Ok());
        }


        [HttpGet("{discordId}/status")]
        public async Task<IActionResult> GetUnitStatuses( [FromRoute] ulong discordId)
        {
            try
            {
                var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {discordId}");
                    return NotFound(new { error = "Unit not found" });
                }

                var statusIds = unit.UnitStatuses.Select(us => us.Id).ToList();

                return Ok(statusIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnitStatuses: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{discordId}/activity")]
        public async Task<IActionResult> GetUnitActivity([FromRoute] ulong id)
        {
            try
            {
                var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == id);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit not found: Discord ID {id}");
                    return NotFound(new { error = "Unit not found" });
                }

                var activityDates = unit.Activities
                    .Select(a => a.Date.ToString("dd.MM.yyyy"))
                    .OrderByDescending(d => d)
                    .ToList();

                return Ok(activityDates);
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

                var unitStatus = unit.UnitStatuses.FirstOrDefault(us => us.Id == statusId);

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
        public async Task<IActionResult> UpdateUnitStatus([FromRoute] ulong id)
        {
            return await Task.FromResult(Ok());
        }


        [HttpPut]
        public async Task<IActionResult> FixUnitActivity()
        {
            return await Task.FromResult(Ok());
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteStatus()
        {
            return await Task.FromResult(Ok());
        }
    }
}