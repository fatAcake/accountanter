using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface IChartOfAccountsService
    {
        Task<List<AccountResponse>> GetAllAsync(bool flat = false);
        Task<AccountResponse?> GetByIdAsync(int id);
        Task<(AccountResponse? account, string? error)> CreateAsync(CreateAccountRequest request);
        Task<(AccountResponse? account, string? error)> UpdateAsync(int id, UpdateAccountRequest request);
        Task<(bool success, string? error)> DeleteAsync(int id);
    }
}
