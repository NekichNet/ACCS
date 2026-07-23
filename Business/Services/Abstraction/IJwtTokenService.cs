using Business.Models;

namespace Business.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(Unit user);
    }
}
