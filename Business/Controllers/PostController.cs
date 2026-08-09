using Business.Models;
using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly PostService _postService;
        private readonly ILogger<PostController> _logger;

        public PostController(PostService postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> CreatePost([FromBody] PostDto dto)
		{
			try
			{
				_postService.Actor = HttpContext.Items["Actor"] as Unit;

				var newPost = await _postService.CreateAsync(
					dto.Name,
					dto.Description,
					dto.SubdivisionId,
					dto.HeadId,
					dto.MaxRankId,
					dto.Color,
					dto.AppendSubdivisionName,
					dto.Permissions.ToList()
				);
				if (!newPost.IsSuccess)
				{
					return BadRequest(new { error = newPost.Message });
				}

				return Ok(newPost.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in CreatePost: {ex.Message}");
				return StatusCode(500, new { error = "Internal server error" });
			}
		}

		[HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var action = await _postService.GetAllAsync();
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
                _logger.LogError($"Error in GetAllPosts: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPost([FromRoute] int postId)
        {
            try
            {
                var action = await _postService.GetAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{postId}/permission")]
        public async Task<IActionResult> GetPostPermissions([FromRoute] int postId)
        {
            try
            {
                var action = await _postService.GetAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                var permissionsIds = action.Value.GetPermissionsRecursive().Select(p => (int)p.Type).ToList();
                return Ok(permissionsIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPostPermissions: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpGet("{postId}/discord-role")]
        public async Task<IActionResult> GetPostDiscordRole([FromRoute] int postId)
        {
            try
            {
                var action = await _postService.GetAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                if (action.Value.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = action.Value.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPostDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost([FromRoute] int postId)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.DeleteAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeletePost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost([FromRoute] int postId, [FromBody] PostDto dto)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.UpdateAsync(postId, dto.Name, dto.AppendSubdivisionName, dto.Description, dto.Color, dto.SubdivisionId, dto.MaxRankId, dto.HeadId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                return Ok(action);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdatePost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{postId}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdatePostRole([FromRoute] int postId)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;
                
                var action = await _postService.UpdateRoleAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                return Ok(action);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdatePostRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{postId}/assign")]
        public async Task<IActionResult> GetAssignedUnits([FromRoute] int postId)
        {
            try
            {
                var action = await _postService.GetUnitsByPostAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(action.Value.Select(u => u.ToDto()));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAssignedUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /*
        [HttpPost("{postId}/assign/{unitId}")] // Todo: переделать на "{postId}/assign/{unitId}"
        [Authorize]
        public async Task<IActionResult> AssignPost([FromRoute] int postId, [FromRoute] ulong unitId, [FromBody] PostDto dto)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.AssignAsync(discordId, postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                return Ok(action.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AssignPost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
        */

        [HttpGet("{postId}/assign/{unitId}")]
        public async Task<IActionResult> GetInfoAssignedUnit([FromRoute] int postId, [FromRoute] ulong unitId)
        {
            try
            {
                var action = await _postService.GetAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "PostId not found" });
                }

                var assignedPost = action.Value.AssignedPosts.FirstOrDefault(ap => ap.Unit.DiscordId == unitId && ap.IsActive());
                if (assignedPost == null)
                {
                    return Ok(null);
                }

                return Ok(assignedPost);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetInfoAssignedUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{postId}/assign/{unitId}")]
        [Authorize]
        public async Task<IActionResult> DeposeUnit([FromRoute] int postId, [FromRoute] ulong unitId)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.DeposeAsync(unitId, postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeposeUnit: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
