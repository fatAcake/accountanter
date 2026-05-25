using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class ReportService : ServiceBase<ReportService>, IReportService
    {
        private readonly IEntityValidationService _validation;
        private readonly IDateTimePeriodService _dates;
        private readonly ISaldoCalculationService _saldo;

        public ReportService(
            IDatabaseContextFactory dbFactory,
            IEntityValidationService validation,
            IDateTimePeriodService dates,
            ISaldoCalculationService saldo,
            ILogger<ReportService> logger)
            : base(dbFactory, logger)
        {
            _validation = validation;
            _dates = dates;
            _saldo = saldo;
        }

        public async Task<ServiceResult<OsvReportResponse>> GetOsvAsync(
            DateTime startDate,
            DateTime endDate,
            int? accountId = null)
        {
            Logger.LogInformation(
                "Формирование ОСВ с {Start} по {End}, accountId={AccountId}",
                startDate, endDate, accountId);

            if (startDate == default || endDate == default)
                return ServiceResult<OsvReportResponse>.Fail(
                    ServiceErrorCode.Validation,
                    "Укажите параметры start_date и end_date.");

            var start = _dates.ToUtcStart(startDate);
            var end = _dates.ToUtcEnd(endDate);

            if (start > end)
                return ServiceResult<OsvReportResponse>.Fail(
                    ServiceErrorCode.Validation,
                    "Дата начала периода не может быть позже даты окончания.");

            await using var db = CreateContext();

            if (accountId is not null)
            {
                var accountError = await _validation.ValidateAccountExistsAsync(db, accountId.Value);
                if (accountError is not null)
                    return ServiceResult<OsvReportResponse>.Fail(accountError);
            }

            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted && (accountId == null || a.id == accountId))
                .OrderBy(a => a.number)
                .ToListAsync();

            var byAccount = await LoadOsvAggregatesAsync(db, start, end);
            var rows = new List<OsvRowResponse>();

            foreach (var account in accounts)
            {
                if (!byAccount.TryGetValue(account.id, out var agg))
                    continue;

                var (openDt, openCt) = _saldo.SplitSaldo(agg.OpeningDebit, agg.OpeningCredit);
                var (closeDt, closeCt) = _saldo.SplitSaldo(
                    agg.OpeningDebit + agg.TurnoverDebit,
                    agg.OpeningCredit + agg.TurnoverCredit);

                var hasActivity = openDt > 0 || openCt > 0 || agg.TurnoverDebit > 0 || agg.TurnoverCredit > 0
                    || closeDt > 0 || closeCt > 0;

                if (!hasActivity)
                    continue;

                rows.Add(new OsvRowResponse
                {
                    account_id = account.id,
                    number = account.number,
                    name = account.name,
                    type = account.type,
                    opening_debit = openDt,
                    opening_credit = openCt,
                    turnover_debit = agg.TurnoverDebit,
                    turnover_credit = agg.TurnoverCredit,
                    closing_debit = closeDt,
                    closing_credit = closeCt,
                });
            }

            Logger.LogInformation("ОСВ сформирована: {RowCount} строк", rows.Count);

            return ServiceResult<OsvReportResponse>.Ok(new OsvReportResponse
            {
                start_date = start,
                end_date = end,
                rows = rows,
                total_opening_debit = rows.Sum(r => r.opening_debit),
                total_opening_credit = rows.Sum(r => r.opening_credit),
                total_turnover_debit = rows.Sum(r => r.turnover_debit),
                total_turnover_credit = rows.Sum(r => r.turnover_credit),
                total_closing_debit = rows.Sum(r => r.closing_debit),
                total_closing_credit = rows.Sum(r => r.closing_credit),
            });
        }

        private static async Task<Dictionary<int, OsvAccountAggregate>> LoadOsvAggregatesAsync(
            Data.ApplicationDbContext db,
            DateTime start,
            DateTime end)
        {
            var map = new Dictionary<int, OsvAccumulator>();

            await AddAggregatesAsync(db, map, start, end, beforePeriod: true, EntrySide.debit, isOpening: true);
            await AddAggregatesAsync(db, map, start, end, beforePeriod: true, EntrySide.credit, isOpening: true);
            await AddAggregatesAsync(db, map, start, end, beforePeriod: false, EntrySide.debit, isOpening: false);
            await AddAggregatesAsync(db, map, start, end, beforePeriod: false, EntrySide.credit, isOpening: false);

            return map.ToDictionary(
                p => p.Key,
                p => new OsvAccountAggregate(
                    p.Key,
                    p.Value.OpeningDebit,
                    p.Value.OpeningCredit,
                    p.Value.TurnoverDebit,
                    p.Value.TurnoverCredit));
        }

        private static async Task AddAggregatesAsync(
            Data.ApplicationDbContext db,
            Dictionary<int, OsvAccumulator> map,
            DateTime start,
            DateTime end,
            bool beforePeriod,
            EntrySide side,
            bool isOpening)
        {
            List<AccountTotalRow> rows;

            if (beforePeriod)
            {
                rows = await (
                    from line in db.TransactionLines.AsNoTracking()
                    join tx in db.Transactions.AsNoTracking() on line.transaction_id equals tx.id
                    where !tx.deleted && line.side == side && tx.date < start
                    group line by line.account_id into g
                    select new AccountTotalRow(
                        g.Key,
                        (decimal)g.Sum(x => (double)x.amount))).ToListAsync();
            }
            else
            {
                rows = await (
                    from line in db.TransactionLines.AsNoTracking()
                    join tx in db.Transactions.AsNoTracking() on line.transaction_id equals tx.id
                    where !tx.deleted && line.side == side && tx.date >= start && tx.date <= end
                    group line by line.account_id into g
                    select new AccountTotalRow(
                        g.Key,
                        (decimal)g.Sum(x => (double)x.amount))).ToListAsync();
            }

            foreach (var row in rows)
            {
                var acc = GetOrCreate(map, row.AccountId);
                if (isOpening)
                    acc.AddOpening(side, row.Total);
                else
                    acc.AddTurnover(side, row.Total);
            }
        }

        private static OsvAccumulator GetOrCreate(Dictionary<int, OsvAccumulator> map, int accountId)
        {
            if (!map.TryGetValue(accountId, out var acc))
            {
                acc = new OsvAccumulator();
                map[accountId] = acc;
            }

            return acc;
        }

        private sealed record AccountTotalRow(int AccountId, decimal Total);

        private sealed record OsvAccountAggregate(
            int AccountId,
            decimal OpeningDebit,
            decimal OpeningCredit,
            decimal TurnoverDebit,
            decimal TurnoverCredit);

        private sealed class OsvAccumulator
        {
            public decimal OpeningDebit { get; private set; }
            public decimal OpeningCredit { get; private set; }
            public decimal TurnoverDebit { get; private set; }
            public decimal TurnoverCredit { get; private set; }

            public void AddOpening(EntrySide side, decimal amount)
            {
                if (side == EntrySide.debit)
                    OpeningDebit += amount;
                else
                    OpeningCredit += amount;
            }

            public void AddTurnover(EntrySide side, decimal amount)
            {
                if (side == EntrySide.debit)
                    TurnoverDebit += amount;
                else
                    TurnoverCredit += amount;
            }
        }
    }
}
