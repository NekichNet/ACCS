using accs.Models.Database;

namespace accs.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(Unit user);
    }
}
