using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllAsync(UserFilter filter);
        Task<UserResponse?> GetByIdAsync(int id);
        Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, int adminUserId);
        Task<ServiceResult<UserResponse>> UpdateAsync(
            int id,
            UpdateUserRequest request,
            int currentUserId);
        Task<ServiceResult> DeleteAsync(int id, int currentUserId);
        Task<List<UserSessionResponse>> GetActiveSessionsAsync();
        Task<ServiceResult> RevokeSessionAsync(int userId, int adminUserId);
        Task<ImportUsersResult> ImportFromCsvAsync(string csvContent, int adminUserId);
    }
}
