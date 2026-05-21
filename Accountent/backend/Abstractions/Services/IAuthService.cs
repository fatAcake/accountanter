using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> RegisterAsync(RegisterRequest request);
        Task<(AuthResponse? response, string? error)> LoginAsync(LoginRequest request);
        Task<(AuthResponse? response, string? error)> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(int userId);
    }
}
