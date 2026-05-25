using backend.Data;
using backend.Models.Results;

namespace backend.Abstractions.Common
{
    public interface IEntityValidationService
    {
        Task<ServiceError?> ValidateDebitCreditAccountsAsync(ApplicationDbContext db, int debitId, int creditId);
        Task<ServiceError?> ValidateCounterpartyExistsAsync(ApplicationDbContext db, int counterpartyId);
        Task<ServiceError?> ValidateAccountExistsAsync(ApplicationDbContext db, int accountId);
        Task<ServiceError?> ValidateAccountsExistAsync(ApplicationDbContext db, IEnumerable<int> accountIds);
        Task<ServiceError?> ValidateInnUniqueAsync(ApplicationDbContext db, string? inn, int? excludeId = null);
    }
}
