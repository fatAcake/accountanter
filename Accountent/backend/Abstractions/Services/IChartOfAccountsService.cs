using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface IChartOfAccountsService
    {
        Task<List<AccountResponse>> GetAllAsync(bool flat = false);
        Task<AccountResponse?> GetByIdAsync(int id);
        Task<ServiceResult<AccountResponse>> CreateAsync(CreateAccountRequest request);
        Task<ServiceResult<AccountResponse>> UpdateAsync(int id, UpdateAccountRequest request);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
