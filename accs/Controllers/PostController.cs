using accs.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;

        public PostController(AppDbContext dbContext, ILogger<UnitController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var posts = await _dbContext.Posts.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DiscordRoleId,
                    p.SubdivisionId,
                    HeadId = p.Head.Id,
                    UnitsCount = p.Units.Count
                }).ToListAsync();

                return Ok(posts);
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
                var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    _logger.LogWarning($"Post not found: Post ID {postId}");
                    return NotFound(new { error = "Post not found" });
                }

                var result = new
                {
                    post.Id,
                    post.Name,
                    post.DiscordRoleId,
                    post.SubdivisionId,
                    HeadId = post.Head?.Id,
                    UnitsIds = post.Units.Select(u => u.DiscordId).ToList(),
                    PermissionsIds = post.GetPermissionsRecursive().Select(p => (int)p.Type).ToList()
                };

                return Ok(result);
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
                var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    _logger.LogWarning($"Post not found: Post ID {postId}");
                    return NotFound(new { error = "Post not found" });
                }

                var permissionsIds = post.GetPermissionsRecursive().Select(p => (int)p.Type).ToList();

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
                var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null)
                {
                    _logger.LogWarning($"Post not found: Post ID {postId}");
                    return NotFound(new { error = "Post not found" });
                }

                if (post.DiscordRoleId == null)
                {
                    return Ok(new { discord_role_id = "" });
                }

                return Ok(new { discord_role_id = post.DiscordRoleId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPostDiscordRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateNewPost()
        {
            return await Task.FromResult(Ok());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost([FromRoute] int id)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePostPermission([FromRoute] int id)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePostRole([FromRoute] int id)
        {
            return await Task.FromResult(Ok());
        }
    }
}
