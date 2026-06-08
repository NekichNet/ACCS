using accs.Models;
using accs.Services.Interfaces;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace accs.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly ILogger<JwtTokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiryMinutes;

        public JwtTokenService(ILogger<JwtTokenService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _jwtSecret = Env.GetString("JWT_SECRET_STRING")
                ?? throw new InvalidOperationException("'JWT_SECRET_STRING' not configured in .env");

            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "https://localhost:3000";
            _jwtAudience = _configuration["Jwt:Audience"] ?? "https://localhost:6061";
            _jwtExpiryMinutes = int.Parse(Env.GetString("JWT_EXPIRY_MINUTES") ?? "60");
        }

        public string GenerateToken(Unit user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.DiscordId.ToString()),
                new Claim(ClaimTypes.Name, user.Nickname),
                new Claim("discord_id", user.DiscordId.ToString()),
            };

            if (user.SteamId.HasValue)
            {
                claims.Add(new Claim("steam_id", user.SteamId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation(
                $"JWT token generated for user {user.Nickname} (Discord ID: {user.DiscordId}), expires in {_jwtExpiryMinutes} minutes");

            return tokenString;
        }
    }
}
