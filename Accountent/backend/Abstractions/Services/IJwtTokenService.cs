using backend.Models;

namespace backend.Abstractions.Services
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) CreateAccessToken(Users user);
        string CreateRefreshToken();
    }
}
