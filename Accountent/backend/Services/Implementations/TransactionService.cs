using System.Globalization;
using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
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

        public async Task<ServiceResult<TransactionResponse>> CreateAsync(
            CreateTransactionRequest request)
        {
            Logger.LogInformation("Создание проводки");

            await using var db = CreateContext();
            var (entity, error) = await BuildTransactionAsync(db, new Transaction(), request);
            if (error is not null)
                return ServiceResult<TransactionResponse>.Fail(error);

            var balanceError = _doubleEntry.GetBalanceError(entity!.lines.ToList());
            if (balanceError is not null)
                return ServiceResult<TransactionResponse>.Fail(
                    ServiceErrorCode.Validation,
                    balanceError);

            await using var dbTx = await db.Database.BeginTransactionAsync();
            db.Transactions.Add(entity);
            await db.SaveChangesAsync();
            await dbTx.CommitAsync();

            Logger.LogInformation("Проводка {TransactionId} создана", entity.id);
            return ServiceResult<TransactionResponse>.Ok(
                await MapSavedTransactionAsync(db, entity.id));
        }

        public async Task<ServiceResult<TransactionResponse>> UpdateAsync(
            int id, UpdateTransactionRequest request)
        {
            Logger.LogInformation("Обновление проводки {TransactionId}", id);

            await using var db = CreateContext();
            var transaction = await db.Transactions
                .Include(t => t.lines)
                .FirstOrDefaultAsync(t => t.id == id && !t.deleted);

            if (transaction is null)
                return ServiceResult<TransactionResponse>.Fail(
                    ServiceErrorCode.NotFound,
                    "Проводка не найдена.");

            db.TransactionLines.RemoveRange(transaction.lines);

            var (entity, error) = await BuildTransactionAsync(db, transaction, request);
            if (error is not null)
                return ServiceResult<TransactionResponse>.Fail(error);

            var balanceError = _doubleEntry.GetBalanceError(entity!.lines.ToList());
            if (balanceError is not null)
                return ServiceResult<TransactionResponse>.Fail(
                    ServiceErrorCode.Validation,
                    balanceError);

            entity.edited_at = DateTime.UtcNow;

            await using var dbTx = await db.Database.BeginTransactionAsync();
            await db.SaveChangesAsync();
            await dbTx.CommitAsync();

            return ServiceResult<TransactionResponse>.Ok(
                await MapSavedTransactionAsync(db, entity.id));
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            Logger.LogInformation("Удаление проводки {TransactionId}", id);

            await using var db = CreateContext();
            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.id == id && !t.deleted);
            if (transaction is null)
                return ServiceResult.Fail(ServiceErrorCode.NotFound, "Проводка не найдена.");

            transaction.deleted = true;
            transaction.deleted_at = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
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

        private async Task<(Transaction? entity, ServiceError? error)> BuildTransactionAsync(
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
                return (null, ServiceErrors.Validation(
                    "Укажите либо простую проводку (debit_account_id, credit_account_id, amount), " +
                    "либо набор строк lines, но не оба варианта одновременно."));
            }

            if (!hasLines && !hasSimple)
                return (null, ServiceErrors.Validation(
                    "Укажите данные проводки: дебет/кредит/сумма или список строк lines."));

            return hasLines
                ? await BuildComplexAsync(db, entity, request)
                : await BuildSimpleAsync(db, entity, request);
        }

        private async Task<(Transaction? entity, ServiceError? error)> BuildSimpleAsync(
            Data.ApplicationDbContext db,
            Transaction entity,
            CreateTransactionRequest request)
        {
            if (request.debit_account_id is null || request.credit_account_id is null || request.amount is null)
                return (null, ServiceErrors.Validation("Укажите счёт дебета, счёт кредита и сумму."));

            if (request.debit_account_id == request.credit_account_id)
                return (null, ServiceErrors.Validation("Счёт дебета и кредита не могут совпадать."));

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

        private async Task<(Transaction? entity, ServiceError? error)> BuildComplexAsync(
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
                return (null, ServiceErrors.Validation(validation.error!));

            entity.is_complex = true;
            entity.amount = validation.debit_total;
            return (entity, null);
        }

        public async Task<ImportTransactionsResult> ImportAsync(ImportTransactionsRequest request)
        {
            var result = new ImportTransactionsResult();
            await using var db = CreateContext();

            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted)
                .ToDictionaryAsync(a => a.number, a => a.id);

            var counterparties = await db.Counterparties
                .AsNoTracking()
                .Where(c => !c.deleted)
                .ToListAsync();

            foreach (var row in request.rows)
            {
                var lineNumber = row.line > 0 ? row.line : 0;

                if (string.IsNullOrWhiteSpace(row.date)
                    || string.IsNullOrWhiteSpace(row.debit_number)
                    || string.IsNullOrWhiteSpace(row.credit_number)
                    || string.IsNullOrWhiteSpace(row.description))
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = "Заполните дату, дебет, кредит, сумму и основание.",
                    });
                    continue;
                }

                if (!TryParseImportDate(row.date, out var date))
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = $"Некорректная дата: {row.date}",
                    });
                    continue;
                }

                if (row.amount <= 0)
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = "Сумма должна быть больше нуля.",
                    });
                    continue;
                }

                var debitKey = row.debit_number.Trim();
                var creditKey = row.credit_number.Trim();
                if (!accounts.TryGetValue(debitKey, out var debitId))
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = $"Счёт дебета не найден: {debitKey}",
                    });
                    continue;
                }

                if (!accounts.TryGetValue(creditKey, out var creditId))
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = $"Счёт кредита не найден: {creditKey}",
                    });
                    continue;
                }

                int? counterpartyId = null;
                if (!string.IsNullOrWhiteSpace(row.counterparty_inn))
                {
                    var inn = row.counterparty_inn.Trim();
                    var cp = counterparties.FirstOrDefault(c =>
                        c.inn != null && c.inn.Equals(inn, StringComparison.OrdinalIgnoreCase));
                    if (cp is null)
                    {
                        result.skipped++;
                        result.errors.Add(new ImportTransactionRowError
                        {
                            line = lineNumber,
                            message = $"Контрагент с ИНН {inn} не найден.",
                        });
                        continue;
                    }

                    counterpartyId = cp.id;
                }
                else if (!string.IsNullOrWhiteSpace(row.counterparty_name))
                {
                    var name = row.counterparty_name.Trim();
                    var cp = counterparties.FirstOrDefault(c =>
                        c.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (cp is null)
                    {
                        result.skipped++;
                        result.errors.Add(new ImportTransactionRowError
                        {
                            line = lineNumber,
                            message = $"Контрагент «{name}» не найден.",
                        });
                        continue;
                    }

                    counterpartyId = cp.id;
                }

                var createRequest = new CreateTransactionRequest
                {
                    date = date,
                    debit_account_id = debitId,
                    credit_account_id = creditId,
                    amount = row.amount,
                    counterparty_id = counterpartyId,
                    description = row.description.Trim(),
                };

                var createResult = await CreateAsync(createRequest);
                if (!createResult.IsSuccess)
                {
                    result.skipped++;
                    result.errors.Add(new ImportTransactionRowError
                    {
                        line = lineNumber,
                        message = createResult.Error?.Message ?? "Не удалось создать проводку.",
                    });
                    continue;
                }

                result.created++;
            }

            Logger.LogInformation(
                "Импорт проводок завершён: создано {Created}, пропущено {Skipped}",
                result.created,
                result.skipped);

            return result;
        }

        private static bool TryParseImportDate(string raw, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var value = raw.Trim();
            var formats = new[]
            {
                "dd.MM.yyyy",
                "dd.MM.yy",
                "yyyy-MM-dd",
                "dd/MM/yyyy",
            };

            if (DateTime.TryParseExact(
                    value,
                    formats,
                    CultureInfo.GetCultureInfo("ru-RU"),
                    DateTimeStyles.None,
                    out date))
                return true;

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out date);
        }

        private async Task<TransactionResponse> MapSavedTransactionAsync(
            Data.ApplicationDbContext db,
            int transactionId) =>
            MapToResponse(await BaseQuery(db).FirstAsync(t => t.id == transactionId));

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
