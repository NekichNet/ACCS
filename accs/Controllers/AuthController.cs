using accs.Database;
using accs.Models;
using accs.Services.Interfaces;
using DotNetEnv;
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
                    _logger.LogError("Failed to get unit data from Discord");
                    return BadRequest(new { error = "Failed to authenticate with Discord" });
                }

                _logger.LogInformation($"Received Discord unit: {discordUser.Username} (ID: {discordUser.Id})");

                if (!ulong.TryParse(discordUser.Id, out var discordId))
                {
                    _logger.LogError($"Failed to parse Discord ID: {discordUser.Id}");
                    return BadRequest(new { error = "Invalid Discord ID format" });
                }

                var temporaryUser = new Unit
                {
                    DiscordId = discordId,
                    Nickname = discordUser.Username
                };

                var jwtToken = _jwtTokenService.GenerateToken(temporaryUser);

                var response = new
                {
                    success = true,
                    access_token = jwtToken,
                    token_type = "Bearer",
                    expires_in = 3600,
                    user = new
                    {
                        discord_id = discordUser.Id,
                        username = temporaryUser.Nickname
                    }
                };

                _logger.LogInformation($"Authentication successful for temporary context of {temporaryUser.Nickname}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Discord callback: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Internal server error during authentication" });
            }
        }


        [HttpGet("discord-login-url")]
        public IActionResult GetDiscordLoginUrl()
        {
            try
            {
                var clientId = Env.GetString("DISCORD_CLIENT_ID")
                    ?? throw new InvalidOperationException("DISCORD_CLIENT_ID undefined");

                var redirectUri = Env.GetString("DISCORD_REDIRECT_URI")
                    ?? throw new InvalidOperationException("DISCORD_REDIRECT_URI undefined");

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


        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var discordIdClaim = User.FindFirst("discord_id")?.Value;

                if (string.IsNullOrEmpty(discordIdClaim) || !ulong.TryParse(discordIdClaim, out var discordId))
                {
                    return Unauthorized(new { error = "Invalid token claims" });
                }

                Unit? unit = await _dbContext.Units.FindAsync(discordId);

                if (unit == null)
                {
                    return StatusCode(403, new { error = "Доступ запрещен. Вы не зарегистрированы в системе." });
                }

                return Ok(new
                {
                    discord_id = unit.DiscordId,
                    username = unit.Nickname,
                    joined = unit.GetRegistrationDateTimeString(),
                    rank = unit.GetRankName(),
                    steam_id = unit.SteamId
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
