namespace accs.Services.Interfaces
{
    public interface IDiscordOAuthService
    {
        Task<DiscordUserData?> GetUserFromCodeAsync(string code);
    }

    public record DiscordUserData(
        string Id,
        string Username,
        string Avatar,
        string? Email = null,
        int Discriminator = 0,
        bool Verified = false
    )
    {
        public string GetAvatarUrl()
        {
            if (string.IsNullOrEmpty(Avatar))
                return "https://cdn.discordapp.com/embed/avatars/0.png";

            string format = Avatar.StartsWith("a_") ? "gif" : "png";
            return $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.{format}";
        }
    }
}
