namespace Business.Services.Interfaces
{
    public interface IDiscordOAuthService
    {
        Task<DiscordUserDTO?> GetUserFromCodeAsync(string code);
    }

    public record DiscordUserDTO(
        string Id,
        string Username,
        string? Email = null,
        int Discriminator = 0,
        bool Verified = false
    )
    {}
}
