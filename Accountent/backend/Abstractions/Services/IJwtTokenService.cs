using backend.Models;
using System.Security.Claims;

namespace backend.Abstractions.Services
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) CreateAccessToken(Users user);
        string CreateRefreshToken();
        ClaimsPrincipal? ValidateAccessToken(string token);
    }
}
