using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class TransactionService : ServiceBase<TransactionService>, ITransactionService
    {
        private readonly IEntityValidationService _validation;
        private readonly IEntityMappingService _mapper;
        private readonly IDoubleEntryValidationService _doubleEntry;
        private readonly IDateTimePeriodService _dates;

        public TransactionService(
            IDatabaseContextFactory dbFactory,
            IEntityValidationService validation,
            IEntityMappingService mapper,
            IDoubleEntryValidationService doubleEntry,
            IDateTimePeriodService dates,
            ILogger<TransactionService> logger)
            : base(dbFactory, logger)
        {
            _validation = validation;
            _mapper = mapper;
            _doubleEntry = doubleEntry;
            _dates = dates;
        }

        public async Task<List<TransactionResponse>> GetAllAsync(TransactionFilter filter)
        {
            Logger.LogDebug("Список проводок");

            await using var db = CreateContext();
            var query = BaseQuery(db);

            if (filter.start_date is not null)
                query = query.Where(t => t.date >= filter.start_date.Value.ToUniversalTime());

            if (filter.end_date is not null)
                query = query.Where(t => t.date <= filter.end_date.Value.ToUniversalTime());

            if (filter.account_id is not null)
            {
                var accountId = filter.account_id.Value;
                query = query.Where(t =>
                    t.debit_account_id == accountId ||
                    t.credit_account_id == accountId ||
                    t.lines.Any(l => l.account_id == accountId));
            }

            if (filter.counterparty_id is not null)
            {
                var counterpartyId = filter.counterparty_id.Value;
                query = query.Where(t =>
                    t.counterparty_id == counterpartyId ||
                    t.lines.Any(l => l.counterparty_id == counterpartyId));
            }

            var items = await query
                .OrderByDescending(t => t.date)
                .ThenByDescending(t => t.id)
                .ToListAsync();

            return items.Select(MapToResponse).ToList();
        }

        public async Task<TransactionResponse?> GetByIdAsync(int id)
        {
            Logger.LogDebug("Проводка {TransactionId}", id);

            await using var db = CreateContext();
            var transaction = await BaseQuery(db).FirstOrDefaultAsync(t => t.id == id);
            return transaction is null ? null : MapToResponse(transaction);
        }

        public async Task<(TransactionResponse? transaction, string? error)> CreateAsync(
            CreateTransactionRequest request)
        {
            Logger.LogInformation("Создание проводки");

            await using var db = CreateContext();
            var (entity, error) = await BuildTransactionAsync(db, new Transaction(), request);
            if (error is not null)
                return (null, error);

            var balanceError = _doubleEntry.GetBalanceError(entity!.lines.ToList());
            if (balanceError is not null)
                return (null, balanceError);

            await using var dbTx = await db.Database.BeginTransactionAsync();
            db.Transactions.Add(entity);
            await db.SaveChangesAsync();
            await dbTx.CommitAsync();

            Logger.LogInformation("Проводка {TransactionId} создана", entity.id);
            return (await GetByIdAsync(entity.id), null);
        }

        public async Task<(TransactionResponse? transaction, string? error)> UpdateAsync(
            int id, UpdateTransactionRequest request)
        {
            Logger.LogInformation("Обновление проводки {TransactionId}", id);

            await using var db = CreateContext();
            var transaction = await db.Transactions
                .Include(t => t.lines)
                .FirstOrDefaultAsync(t => t.id == id && !t.deleted);

            if (transaction is null)
                return (null, "Проводка не найдена.");

            db.TransactionLines.RemoveRange(transaction.lines);

            var (entity, error) = await BuildTransactionAsync(db, transaction, request);
            if (error is not null)
                return (null, error);

            var balanceError = _doubleEntry.GetBalanceError(entity!.lines.ToList());
            if (balanceError is not null)
                return (null, balanceError);

            entity.edited_at = DateTime.UtcNow;

            await using var dbTx = await db.Database.BeginTransactionAsync();
            await db.SaveChangesAsync();
            await dbTx.CommitAsync();

            return (await GetByIdAsync(entity.id), null);
        }

        public async Task<(bool success, string? error)> DeleteAsync(int id)
        {
            Logger.LogInformation("Удаление проводки {TransactionId}", id);

            await using var db = CreateContext();
            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.id == id && !t.deleted);
            if (transaction is null)
                return (false, "Проводка не найдена.");

            transaction.deleted = true;
            transaction.deleted_at = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return (true, null);
        }

        private static IQueryable<Transaction> BaseQuery(Data.ApplicationDbContext db) =>
            db.Transactions
                .AsNoTracking()
                .Where(t => !t.deleted)
                .Include(t => t.debit_account)
                .Include(t => t.credit_account)
                .Include(t => t.counterparty)
                .Include(t => t.lines).ThenInclude(l => l.account)
                .Include(t => t.lines).ThenInclude(l => l.counterparty);

        private async Task<(Transaction? entity, string? error)> BuildTransactionAsync(
            Data.ApplicationDbContext db,
            Transaction entity,
            CreateTransactionRequest request)
        {
            entity.date = _dates.ToUtc(request.date);
            entity.description = request.description.Trim();
            entity.debit_account_id = null;
            entity.credit_account_id = null;
            entity.amount = null;
            entity.counterparty_id = null;
            entity.lines.Clear();

            var hasLines = request.lines is { Count: > 0 };
            var hasSimple = request.debit_account_id is not null
                || request.credit_account_id is not null
                || request.amount is not null;

            if (hasLines && hasSimple)
            {
                return (null, "Укажите либо простую проводку (debit_account_id, credit_account_id, amount), " +
                    "либо набор строк lines, но не оба варианта одновременно.");
            }

            if (!hasLines && !hasSimple)
                return (null, "Укажите данные проводки: дебет/кредит/сумма или список строк lines.");

            return hasLines
                ? await BuildComplexAsync(db, entity, request)
                : await BuildSimpleAsync(db, entity, request);
        }

        private async Task<(Transaction? entity, string? error)> BuildSimpleAsync(
            Data.ApplicationDbContext db,
            Transaction entity,
            CreateTransactionRequest request)
        {
            if (request.debit_account_id is null || request.credit_account_id is null || request.amount is null)
                return (null, "Укажите счёт дебета, счёт кредита и сумму.");

            if (request.debit_account_id == request.credit_account_id)
                return (null, "Счёт дебета и кредита не могут совпадать.");

            var accountError = await _validation.ValidateDebitCreditAccountsAsync(
                db, request.debit_account_id.Value, request.credit_account_id.Value);
            if (accountError is not null)
                return (null, accountError);

            if (request.counterparty_id is not null)
            {
                var cpError = await _validation.ValidateCounterpartyExistsAsync(
                    db, request.counterparty_id.Value);
                if (cpError is not null)
                    return (null, cpError);
            }

            entity.is_complex = false;
            entity.debit_account_id = request.debit_account_id;
            entity.credit_account_id = request.credit_account_id;
            entity.amount = request.amount;
            entity.counterparty_id = request.counterparty_id;

            entity.lines.Add(new TransactionLine
            {
                account_id = request.debit_account_id.Value,
                amount = request.amount.Value,
                side = EntrySide.debit,
                counterparty_id = request.counterparty_id,
            });
            entity.lines.Add(new TransactionLine
            {
                account_id = request.credit_account_id.Value,
                amount = request.amount.Value,
                side = EntrySide.credit,
            });

            return (entity, null);
        }

        private async Task<(Transaction? entity, string? error)> BuildComplexAsync(
            Data.ApplicationDbContext db,
            Transaction entity,
            CreateTransactionRequest request)
        {
            var inputLines = request.lines!;
            var accountIds = inputLines.Select(l => l.account_id).Distinct();

            var accountsError = await _validation.ValidateAccountsExistAsync(db, accountIds);
            if (accountsError is not null)
                return (null, accountsError);

            foreach (var line in inputLines)
            {
                if (line.counterparty_id is not null)
                {
                    var cpError = await _validation.ValidateCounterpartyExistsAsync(
                        db, line.counterparty_id.Value);
                    if (cpError is not null)
                        return (null, cpError);
                }

                entity.lines.Add(new TransactionLine
                {
                    account_id = line.account_id,
                    amount = line.amount,
                    side = line.side,
                    counterparty_id = line.counterparty_id,
                });
            }

            var validation = _doubleEntry.Validate(entity.lines.ToList());
            if (!validation.is_balanced)
                return (null, validation.error);

            entity.is_complex = true;
            entity.amount = validation.debit_total;
            return (entity, null);
        }

        private TransactionResponse MapToResponse(Transaction t)
        {
            var validation = _doubleEntry.Validate(t.lines.ToList());

            return new TransactionResponse
            {
                id = t.id,
                date = t.date,
                amount = t.amount ?? validation.debit_total,
                description = t.description,
                is_complex = t.is_complex,
                is_balanced = validation.is_balanced,
                debit_total = validation.debit_total,
                credit_total = validation.credit_total,
                debit_account_id = t.debit_account_id,
                debit_account = t.debit_account is null ? null : _mapper.MapAccount(t.debit_account),
                credit_account_id = t.credit_account_id,
                credit_account = t.credit_account is null ? null : _mapper.MapAccount(t.credit_account),
                counterparty_id = t.counterparty_id,
                counterparty = t.counterparty is null ? null : _mapper.MapCounterparty(t.counterparty),
                lines = t.lines.OrderBy(l => l.id).Select(l => new TransactionLineResponse
                {
                    id = l.id,
                    account_id = l.account_id,
                    account = _mapper.MapAccount(l.account),
                    amount = l.amount,
                    side = l.side,
                    counterparty_id = l.counterparty_id,
                    counterparty = l.counterparty is null ? null : _mapper.MapCounterparty(l.counterparty),
                }).ToList(),
            };
        }
    }
}
