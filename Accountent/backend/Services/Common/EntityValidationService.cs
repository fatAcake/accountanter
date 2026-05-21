using backend.Abstractions.Common;
using backend.Data;
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

        public async Task<string?> ValidateDebitCreditAccountsAsync(
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
                return "Счёт дебета не найден.";
            }

            if (!existing.Contains(creditId))
            {
                _logger.LogWarning("Счёт кредита {AccountId} не найден", creditId);
                return "Счёт кредита не найден.";
            }

            return null;
        }

        public async Task<string?> ValidateCounterpartyExistsAsync(ApplicationDbContext db, int counterpartyId)
        {
            var exists = await db.Counterparties.AnyAsync(c => c.id == counterpartyId && !c.deleted);
            if (!exists)
                _logger.LogWarning("Контрагент {CounterpartyId} не найден", counterpartyId);
            return exists ? null : "Контрагент не найден.";
        }

        public async Task<string?> ValidateAccountExistsAsync(ApplicationDbContext db, int accountId)
        {
            var exists = await db.Accounts.AnyAsync(a => a.id == accountId && !a.deleted);
            if (!exists)
                _logger.LogWarning("Счёт {AccountId} не найден", accountId);
            return exists ? null : "Счёт не найден.";
        }

        public async Task<string?> ValidateAccountsExistAsync(
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
            return $"Счёт {missing} не найден.";
        }

        public async Task<string?> ValidateInnUniqueAsync(
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

            return exists ? "Контрагент с таким ИНН уже существует." : null;
        }
    }
}
