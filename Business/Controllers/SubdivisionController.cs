using Business.Models;
using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SubdivisionController : ControllerBase
    {
        private readonly SubdivisionService _subdivisionService;
        private readonly ILogger<SubdivisionController> _logger;

        public SubdivisionController(SubdivisionService subdivisionService, ILogger<SubdivisionController> logger)
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
                if (!subdivisions.IsSuccess)
                {
                    return BadRequest(new { error = subdivisions.Message });
                }
                if (subdivisions.Value == null)
                {
                    return StatusCode(500, new { error = "Empry list of subdivisions" });
                }

                var res = subdivisions;

                return Ok(res.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllSubdivisions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{subdivisionId}")]
        public async Task<IActionResult> GetSubdivision([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivision = await _subdivisionService.GetAsync(subdivisionId);
                if (!subdivision.IsSuccess)
                {
                    return BadRequest(new { error = subdivision.Message });
                }
                return Ok(subdivision.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{subdivisionId}/permission")]
        public async Task<IActionResult> GetSubdivisionPermissions([FromRoute] int subdivisionId)
        {
            try
            {
                var subdivisions = await _subdivisionService.GetAsync(subdivisionId);
                if (!subdivisions.IsSuccess)
                {
                    return BadRequest(new { error = subdivisions.Message });
                }
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
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }
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
        [Authorize]
        public async Task<IActionResult> CreateSubdivision([FromBody] SubdivisionDto dto)
        {
            try
            {
                _subdivisionService.Actor = HttpContext.Items["Actor"] as Unit;

                var subdivision = await _subdivisionService.CreateAsync(
                    dto.Name,
                    dto.AppendHeadName,
                    dto.Description,
                    dto.Color,
                    dto.DiscordRoleId,
                    dto.HeadId);
                if (!subdivision.IsSuccess)
                {
                    return BadRequest(new { error = subdivision.Message });
                }
                if (subdivision.Value == null)
                {
                    return BadRequest(new { error = "Subdivision undefined. May be u haven't permission" });
                }

                return Ok(subdivision.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{subdivisionId}")]
        [Authorize]
        public async Task<IActionResult> UpdateSubdivision([FromRoute] int subdivisionId, [FromBody]SubdivisionDto dto)
        {
            try
            {
                _subdivisionService.Actor = HttpContext.Items["Actor"] as Unit;

                var subdivision = await _subdivisionService.UpdateAsync(subdivisionId, dto.Name, dto.Color, dto.HeadId);
                if (!subdivision.IsSuccess)
                {
                    return BadRequest(new { error = subdivision.Message });
                }
                return Ok(subdivision);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{subdivisionId}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdateSubdivisionRole([FromRoute] int subdivisionId)
        {
            try
            {
                _subdivisionService.Actor = HttpContext.Items["Actor"] as Unit;

                var subdivision = await _subdivisionService.UpdateRoleAsync(subdivisionId);
                if (!subdivision.IsSuccess)
                {
                    return BadRequest(new { error = subdivision.Message });
                }
                return Ok(subdivision.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateSubdivisionRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{subdivisionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteSubdivision([FromRoute] int subdivisionId)
        {
            try
            {
                _subdivisionService.Actor = HttpContext.Items["Actor"] as Unit;

                var subdivision = await _subdivisionService.DeleteAsync(subdivisionId);
                if (!subdivision.IsSuccess)
                {
                    return BadRequest(new { error = subdivision.Message });
                }

                return Ok(subdivision);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteSubdivision: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}