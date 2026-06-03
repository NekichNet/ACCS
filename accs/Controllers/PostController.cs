using accs.Database;
using accs.Models;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/[controller]")]
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
                if (action.Value == null)
                {
                    return NotFound(new { error = "Post not found" });
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
                    return NotFound(new { error = "Post not found" });
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
                    return NotFound(new { error = "Post not found" });
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


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateNewPost([FromBody] PostDto dto)
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
                    dto.PermissionsId
                );
                if (!newPost.IsSuccess)
                {
                    return BadRequest(new { error = newPost.Message });
                }

                return Ok(newPost.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateNewPost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost([FromRoute] int id)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.DeleteAsync(id);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action == null)
                {
                    return NotFound(new { error = "Post not found" });
                }

                return Ok(new { message = action.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeletePost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost([FromRoute] int id, [FromBody] PostDto dto)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.UpdateAsync(id, dto.Name, dto.AppendSubdivisionName, dto.Description, dto.Color, dto.SubdivisionId, dto.MaxRankId, dto.HeadId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action == null)
                {
                    return NotFound(new { error = "Post not found" });
                }

                return Ok(action);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdatePost: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{id}/discord-role")]
        [Authorize]
        public async Task<IActionResult> UpdatePostRole([FromRoute] int id, [FromBody] PostDto dto)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;
                
                var action = await _postService.UpdateRoleAsync(id);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Post not found" });
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
                var action = await _postService.GetAsync(postId);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Post not found" });
                }

                var assignedPosts = action.Value.AssignedPosts.Where(ap => ap.IsActive()).ToList();
                return Ok(assignedPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAssignedUnits: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("{postId}/assign")]
        [Authorize]
        public async Task<IActionResult> AssignPost([FromRoute] int postId, [FromBody] PostDto dto)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.AssignAsync(dto.DiscordId, postId);
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

        [HttpGet("{id}/assign/{discordId}")]
        public async Task<IActionResult> GetInfoAssignedUnit([FromRoute] int id, [FromRoute] ulong discordId)
        {
            try
            {
                var action = await _postService.GetAsync(id);
                if (!action.IsSuccess)
                {
                    return BadRequest(new { error = action.Message });
                }
                if (action.Value == null)
                {
                    return NotFound(new { error = "Post not found" });
                }

                var assignedPost = action.Value.AssignedPosts.FirstOrDefault(ap => ap.Unit.DiscordId == discordId && ap.IsActive());
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

        [HttpDelete("{postId}/assign/{discordId}")]
        [Authorize]
        public async Task<IActionResult> DeposeUnit([FromRoute] int postId, [FromRoute] ulong discordId)
        {
            try
            {
                _postService.Actor = HttpContext.Items["Actor"] as Unit;

                var action = await _postService.DeposeAsync(discordId, postId);
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

    public class PostDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? SubdivisionId { get; set; }
        public int HeadId { get; set; }
        public int MaxRankId { get; set; }
        public string Color { get; set; } = string.Empty;
        public bool AppendSubdivisionName { get; set; }
        public List<int> PermissionsId { get; set; } = new List<int>();
        public ulong DiscordId { get; set; }
    }
}
