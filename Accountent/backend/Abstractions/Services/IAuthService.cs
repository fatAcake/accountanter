using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(int userId);
    }
}
