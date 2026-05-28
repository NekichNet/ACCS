using accs.Database;
using accs.Models;
using accs.Services.Interfaces;
using Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace accs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IDiscordOAuthService _discordOAuthService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IDiscordOAuthService discordOAuthService,
            IJwtTokenService jwtTokenService,
            AppDbContext dbContext,
            ILogger<AuthController> logger)
        {
            _discordOAuthService = discordOAuthService;
            _jwtTokenService = jwtTokenService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("discord-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> DiscordCallback([FromQuery] string code, [FromQuery] string? state)
        {
            try
            {
                _logger.LogInformation("Processing Discord OAuth callback");

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("Discord callback received without authorization code");
                    return BadRequest(new { error = "Authorization code is missing" });
                }

                var discordUser = await _discordOAuthService.GetUserFromCodeAsync(code);

                if (discordUser == null)
                {
                    _logger.LogError("Failed to get user data from Discord");
                    return BadRequest(new { error = "Failed to authenticate with Discord" });
                }

                _logger.LogInformation(
                    $"Received Discord user: {discordUser.Username} (ID: {discordUser.Id})");

                if (!ulong.TryParse(discordUser.Id, out var discordId))
                {
                    _logger.LogError($"Failed to parse Discord ID: {discordUser.Id}");
                    return BadRequest(new { error = "Invalid Discord ID format" });
                }

                var existingUser = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == discordId);

                Unit user;

                if (existingUser != null)
                {
                    _logger.LogInformation(
                        $"User already exists in database: {existingUser.Nickname}");

                    if (existingUser.Nickname != discordUser.Username)
                    {
                        existingUser.Nickname = discordUser.Username;
                        _dbContext.Units.Update(existingUser);
                    }

                    user = existingUser;
                }
                else
                {
                    _logger.LogInformation(
                        $"Creating new user in database: {discordUser.Username}");

                    user = new Unit
                    {
                        DiscordId = discordId,
                        Nickname = discordUser.Username,
                        Joined = DateTime.UtcNow
                    };

                    _dbContext.Units.Add(user);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"User saved to database. Discord ID: {discordId}");

                var jwtToken = _jwtTokenService.GenerateToken(user);

                var response = new
                {
                    success = true,
                    access_token = jwtToken,
                    token_type = "Bearer",
                    expires_in = 3600,
                    user = new
                    {
                        discord_id = discordId,
                        username = user.Nickname
                    }
                };

                _logger.LogInformation(
                    $"Authentication successful for {user.Nickname}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Discord callback: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Internal server error during authentication" });
            }
        }

        [HttpGet("discord-login-url")]
        [AllowAnonymous]
        public IActionResult GetDiscordLoginUrl()
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
                var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                {
                    _logger.LogError("Discord OAuth not configured: missing CLIENT_ID or REDIRECT_URI");
                    return BadRequest(new { error = "Discord OAuth not configured" });
                }

                var state = Guid.NewGuid().ToString("N");

                var loginUrl = $"https://discord.com/api/oauth2/authorize?" +
                    $"client_id={clientId}" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&response_type=code" +
                    $"&scope=identify" +
                    $"&state={state}";

                _logger.LogInformation("Generated Discord login URL");

                return Ok(new
                {
                    login_url = loginUrl,
                    state = state
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating Discord login URL: {ex.Message}");
                return StatusCode(500, new { error = "Failed to generate login URL" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var discordIdClaim = User.FindFirst("discord_id")?.Value;

                if (string.IsNullOrEmpty(discordIdClaim) || !ulong.TryParse(discordIdClaim, out var discordId))
                {
                    _logger.LogWarning("Invalid discord_id claim in JWT token");
                    return Unauthorized(new { error = "Invalid token" });
                }

                var user = await _dbContext.Units.FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found: Discord ID {discordId}");
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new
                {
                    discord_id = user.DiscordId,
                    username = user.Nickname,
                    joined = user.Joined,
                    rank = user.Rank?.Name,
                    steam_id = user.SteamId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCurrentUser: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
