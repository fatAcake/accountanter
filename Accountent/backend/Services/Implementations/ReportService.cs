using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
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

        public async Task<(OsvReportResponse? report, string? error)> GetOsvAsync(
            DateTime startDate,
            DateTime endDate,
            int? accountId = null)
        {
            Logger.LogInformation(
                "Формирование ОСВ с {Start} по {End}, accountId={AccountId}",
                startDate, endDate, accountId);

            if (startDate == default || endDate == default)
                return (null, "Укажите параметры start_date и end_date.");

            var start = _dates.ToUtcStart(startDate);
            var end = _dates.ToUtcEnd(endDate);

            if (start > end)
                return (null, "Дата начала периода не может быть позже даты окончания.");

            await using var db = CreateContext();

            if (accountId is not null)
            {
                var accountError = await _validation.ValidateAccountExistsAsync(db, accountId.Value);
                if (accountError is not null)
                    return (null, accountError);
            }

            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted && (accountId == null || a.id == accountId))
                .OrderBy(a => a.number)
                .ToListAsync();

            var lines = await db.TransactionLines
                .AsNoTracking()
                .Where(l => !l.transaction.deleted)
                .Select(l => new LineSnapshot(
                    l.account_id,
                    l.side,
                    l.amount,
                    l.transaction.date))
                .ToListAsync();

            var rows = new List<OsvRowResponse>();

            foreach (var account in accounts)
            {
                var accountLines = lines.Where(l => l.AccountId == account.id).ToList();

                var openingDebit = accountLines
                    .Where(l => l.Date < start && l.Side == EntrySide.debit)
                    .Sum(l => l.Amount);
                var openingCredit = accountLines
                    .Where(l => l.Date < start && l.Side == EntrySide.credit)
                    .Sum(l => l.Amount);

                var turnoverDebit = accountLines
                    .Where(l => l.Date >= start && l.Date <= end && l.Side == EntrySide.debit)
                    .Sum(l => l.Amount);
                var turnoverCredit = accountLines
                    .Where(l => l.Date >= start && l.Date <= end && l.Side == EntrySide.credit)
                    .Sum(l => l.Amount);

                var (openDt, openCt) = _saldo.SplitSaldo(openingDebit, openingCredit);

                var totalDebit = openingDebit + turnoverDebit;
                var totalCredit = openingCredit + turnoverCredit;
                var (closeDt, closeCt) = _saldo.SplitSaldo(totalDebit, totalCredit);

                var hasActivity = openDt > 0 || openCt > 0 || turnoverDebit > 0 || turnoverCredit > 0
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
                    turnover_debit = turnoverDebit,
                    turnover_credit = turnoverCredit,
                    closing_debit = closeDt,
                    closing_credit = closeCt,
                });
            }

            Logger.LogInformation("ОСВ сформирована: {RowCount} строк", rows.Count);

            return (new OsvReportResponse
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
            }, null);
        }

        private sealed record LineSnapshot(int AccountId, EntrySide Side, decimal Amount, DateTime Date);
    }
}
