using Business.Services.Interfaces;
using DotNetEnv;
using System.Text.Json;

namespace Business.Services
{
    public class DiscordOAuthService : IDiscordOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiscordOAuthService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public DiscordOAuthService(
            HttpClient httpClient,
            ILogger<DiscordOAuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _clientId = Env.GetString("DISCORD_CLIENT_ID")
                ?? throw new InvalidOperationException("'DISCORD_CLIENT_ID' not configured in .env");

            _clientSecret = Env.GetString("DISCORD_CLIENT_SECRET")
                ?? throw new InvalidOperationException("'DISCORD_CLIENT_SECRET' not configured in .env");

            _redirectUri = Env.GetString("DISCORD_REDIRECT_URI")
                ?? throw new InvalidOperationException("'DISCORD_REDIRECT_URI' not configured in .env");
        }

        public async Task<DiscordUserDTO?> GetUserFromCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Discord OAuth code is null or empty");
                return null;
            }

            try
            {
                _logger.LogInformation("Starting Discord OAuth token exchange");

                var accessToken = await ExchangeCodeForTokenAsync(code);
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to get access token from Discord");
                    return null;
                }

                var user = await GetUserInfoAsync(accessToken);

                if (user != null)
                {
                    _logger.LogInformation(
                        $"Successfully authenticated Discord user: {user.Username} (ID: {user.Id})");
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during Discord OAuth: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", _redirectUri }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync("https://discord.com/api/oauth2/token", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        $"Failed to exchange code for token. Status: {response.StatusCode}, Content: {errorContent}");
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("access_token", out var accessTokenElement))
                    {
                        return accessTokenElement.GetString();
                    }
                }

                _logger.LogError("No access_token in Discord response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exchanging code for token: {ex.Message}");
                return null;
            }
        }

        private async Task<DiscordUserDTO?> GetUserInfoAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        $"Failed to get user info. Status: {response.StatusCode}, Content: {errorContent}");
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;

                    var user = new DiscordUserDTO(
                        Id: root.GetProperty("id").GetString() ?? "",
                        Username: root.GetProperty("username").GetString() ?? "",
                        Email: root.TryGetProperty("email", out var email) ? email.GetString() : null,
                        Discriminator: root.TryGetProperty("discriminator", out var disc)
                            ? int.TryParse(disc.GetString(), out var d) ? d : 0
                            : 0,
                        Verified: root.TryGetProperty("verified", out var verified) ? verified.GetBoolean() : false
                    );

                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user info from Discord: {ex.Message}");
                return null;
            }
        }
    }
}
