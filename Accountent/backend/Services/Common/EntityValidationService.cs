using backend.Abstractions.Common;
using backend.Data;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Common
{
    public sealed class EntityValidationService : IEntityValidationService
    {
        private readonly ILogger<EntityValidationService> _logger;

        public EntityValidationService(ILogger<EntityValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<ServiceError?> ValidateDebitCreditAccountsAsync(
            ApplicationDbContext db, int debitId, int creditId)
        {
            var ids = new[] { debitId, creditId };
            var existing = await db.Accounts
                .Where(a => ids.Contains(a.id) && !a.deleted)
                .Select(a => a.id)
                .ToListAsync();

            if (!existing.Contains(debitId))
            {
                _logger.LogWarning("Счёт дебета {AccountId} не найден", debitId);
                return ServiceErrors.NotFound("Счёт дебета не найден.");
            }

            if (!existing.Contains(creditId))
            {
                _logger.LogWarning("Счёт кредита {AccountId} не найден", creditId);
                return ServiceErrors.NotFound("Счёт кредита не найден.");
            }

            return null;
        }

        public async Task<ServiceError?> ValidateCounterpartyExistsAsync(ApplicationDbContext db, int counterpartyId)
        {
            var exists = await db.Counterparties.AnyAsync(c => c.id == counterpartyId && !c.deleted);
            if (!exists)
                _logger.LogWarning("Контрагент {CounterpartyId} не найден", counterpartyId);
            return exists ? null : ServiceErrors.NotFound("Контрагент не найден.");
        }

        public async Task<ServiceError?> ValidateAccountExistsAsync(ApplicationDbContext db, int accountId)
        {
            var exists = await db.Accounts.AnyAsync(a => a.id == accountId && !a.deleted);
            if (!exists)
                _logger.LogWarning("Счёт {AccountId} не найден", accountId);
            return exists ? null : ServiceErrors.NotFound("Счёт не найден.");
        }

        public async Task<ServiceError?> ValidateAccountsExistAsync(
            ApplicationDbContext db, IEnumerable<int> accountIds)
        {
            var ids = accountIds.Distinct().ToList();
            var existing = await db.Accounts
                .Where(a => ids.Contains(a.id) && !a.deleted)
                .Select(a => a.id)
                .ToListAsync();

            var missing = ids.Except(existing).FirstOrDefault();
            if (missing == 0)
                return null;

            _logger.LogWarning("Счёт {AccountId} не найден", missing);
            return ServiceErrors.NotFound($"Счёт {missing} не найден.");
        }

        public async Task<ServiceError?> ValidateInnUniqueAsync(
            ApplicationDbContext db, string? inn, int? excludeId = null)
        {
            if (inn is null)
                return null;

            var exists = await db.Counterparties.AnyAsync(c =>
                !c.deleted &&
                c.inn == inn &&
                (excludeId == null || c.id != excludeId));

            if (exists)
                _logger.LogWarning("Дубликат ИНН {Inn}", inn);

            return exists
                ? ServiceErrors.Conflict("Контрагент с таким ИНН уже существует.")
                : null;
        }
    }
}
