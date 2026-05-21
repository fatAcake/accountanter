using backend.Data;

namespace backend.Abstractions.Common
{
    public interface IEntityValidationService
    {
        Task<string?> ValidateDebitCreditAccountsAsync(ApplicationDbContext db, int debitId, int creditId);
        Task<string?> ValidateCounterpartyExistsAsync(ApplicationDbContext db, int counterpartyId);
        Task<string?> ValidateAccountExistsAsync(ApplicationDbContext db, int accountId);
        Task<string?> ValidateAccountsExistAsync(ApplicationDbContext db, IEnumerable<int> accountIds);
        Task<string?> ValidateInnUniqueAsync(ApplicationDbContext db, string? inn, int? excludeId = null);
    }
}
