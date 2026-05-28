using accs.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RankController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RankController> _logger;

        public RankController(AppDbContext dbContext, ILogger<RankController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRanks()
        {
            try
            {
                var ranks = await _dbContext.Ranks.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.DiscordRoleId,
                    UnitsCount = r.Units.Count
                }).ToListAsync();

                return Ok(ranks);
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
                var rank = await _dbContext.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);

                if (rank == null)
                {
                    _logger.LogWarning($"Rank not found: Rank ID {rankId}");
                    return NotFound(new { error = "Rank not found" });
                }

                var result = new
                {
                    rank.Id,
                    rank.Name,
                    rank.DiscordRoleId,
                    UnitsIds = rank.Units.Select(u => u.DiscordId).ToList(),
                    PermissionsIds = rank.GetPermissionsRecursive().Select(p => (int)p.Type).ToList(),
                    PreviousRankId = rank.Previous?.Id,
                    NextRankId = rank.Next?.Id
                };

                return Ok(result);
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
                var rank = await _dbContext.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);

                if (rank == null)
                {
                    _logger.LogWarning($"Rank not found: Rank ID {rankId}");
                    return NotFound(new { error = "Rank not found" });
                }

                var permissionsIds = rank.GetPermissionsRecursive().Select(p => (int)p.Type).ToList();

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
                var rank = await _dbContext.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);

                if (rank == null)
                {
                    _logger.LogWarning($"Rank not found: Rank ID {rankId}");
                    return NotFound(new { error = "Rank not found" });
                }

                if (rank.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = rank.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRankDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateNewRank()
        {
            return await Task.FromResult(Ok());
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRank()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateRank()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscordRoleRank()
        {
            return await Task.FromResult(Ok());
        }
    }
}