using accs.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubdivisionController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;

        public SubdivisionController(AppDbContext dbContext, ILogger<UnitController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubdivisions()
        {
            try
            {
                var subdivisions = await _dbContext.Subdivisions.ToDictionaryAsync(
                    s => s.Name,
                    s => new
                    {
                        s.Id,
                        s.Color,
                        HeadSubdivisionId = s.Head?.Id
                    }
                );

                return Ok(subdivisions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllSubdivisions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{subdivisionId}")]
        public async Task<IActionResult> GetSubdivisionById([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivision = await _dbContext.Subdivisions.FirstOrDefaultAsync(s => s.Id == subdivisionId);

                if (subdivision == null)
                {
                    _logger.LogWarning($"Subdivision not found: Subdivision ID {subdivisionId}");
                    return NotFound(new { error = "Subdivision not found" });
                }

                var result = new
                {
                    subdivision.Id,
                    subdivision.Name,
                    subdivision.Color,
                    HeadSubdivisionId = subdivision.Head?.Id,
                    PostsIds = subdivision.Posts.Select(p => p.Id).ToList(),
                    PermissionsIds = subdivision.GetPermissionsRecursive().Select(p => (int)p.Type).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSubdivisionById: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{subdivisionId}/permission")]
        public async Task<IActionResult> GetSubdivisionPermissions([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivision = await _dbContext.Subdivisions.FirstOrDefaultAsync(s => s.Id == subdivisionId);

                if (subdivision == null)
                {
                    _logger.LogWarning($"Subdivision not found: Subdivision ID {subdivisionId}");
                    return NotFound(new { error = "Subdivision not found" });
                }

                var permissionsIds = subdivision.GetPermissionsRecursive().Select(p => (int)p.Type).ToList();

                return Ok(permissionsIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSubdivisionPermissions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{subdivisionId}/discord-role")]
        public async Task<IActionResult> GetSubdivisionDiscordRole([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivision = await _dbContext.Subdivisions.FirstOrDefaultAsync(s => s.Id == subdivisionId);

                if (subdivision == null)
                {
                    _logger.LogWarning($"Subdivision not found: Subdivision ID {subdivisionId}");
                    return NotFound(new { error = "Subdivision not found" });
                }

                if (subdivision.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = subdivision.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSubdivisionDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewSubdivision()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateSubdivision()
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRoleSubdivision()
        {
            return await Task.FromResult(Ok());
        }
    }
}