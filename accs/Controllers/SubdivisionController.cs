using accs.Database;
using accs.Models;
using accs.Services;
using Discord.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sprache;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubdivisionController : ControllerBase
    {
        private readonly SubdivisionService _subdivisionService;
        private readonly ILogger<UnitController> _logger;

        public SubdivisionController(SubdivisionService subdivisionService, ILogger<UnitController> logger)
        {
            _subdivisionService = subdivisionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubdivisions()
        {
            try
            {
                var subdivisions = await _subdivisionService.GetAllAsync();
                if (subdivisions.Value == null)
                {
                    return StatusCode(500, new { error = "Empry list of subdivisions" });
                }

                var res = subdivisions.Value.ToDictionary(
                    s => s.Name,
                    s => new
                    {
                        Id = s.Id,
                        Color = s.Color,
                        HeadSubdivisionId = s.HeadId
                    }
                );

                return Ok(res);
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
                var subdivision = await _subdivisionService.GetAsync(subdivisionId);
                return Ok(subdivision);
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
                var subdivisions = await _subdivisionService.GetAsync(subdivisionId);
                if (subdivisions.Value == null)
                {
                    return NotFound(new { error = "Subdivision undefined" });
                }

                var permissions = subdivisions.Value.GetPermissionsRecursive();
                return Ok(permissions);
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
                var result = await _subdivisionService.GetAsync(subdivisionId);
                if (result.Value == null)
                {
                    return NotFound(new { error = "Subdivision undefined" });
                }

                var discordRoleId = result.Value.DiscordRoleId;
                return Ok(new { discord_role_id = discordRoleId?.ToString() ?? "" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSubdivisionDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewSubdivision([FromBody] string name, string? envRoleString = null, int? headId = null)
        {
            try
            {
                var subdivision = await _subdivisionService.CreateAsync(name, envRoleString, headId);
                if (subdivision.Value == null)
                {
                    return BadRequest(new { error = "Subdivision undefined. May be u haven't permission" });
                }

                return Ok(subdivision.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateNewSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{subdivisionId}")]
        public async Task<IActionResult> UpdateSubdivision([FromRoute] int id,
            [FromBody] string name,
            [FromBody] string? color = null,
            [FromBody] int? headId = null)
        {
            try
            {
                var subdivision = await _subdivisionService.UpdateAsync(id, name, color, headId);
                return Ok(subdivision);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{subdivisionId}/discord-role")]
        public async Task<IActionResult> UpdateRoleSubdivision([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivision = await _subdivisionService.UpdateRoleAsync(subdivisionId);
                return Ok(subdivision);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateRoleSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}