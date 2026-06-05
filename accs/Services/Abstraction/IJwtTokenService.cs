using accs.Models;

namespace accs.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(Unit user);
    }
}
