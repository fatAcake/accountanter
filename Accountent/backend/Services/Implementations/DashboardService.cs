using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class DashboardService : ServiceBase<DashboardService>, IDashboardService
    {
        private static readonly string[] IncomePrefixes = ["90.01", "91.01"];
        private static readonly string[] ExpensePrefixes = ["90.02", "91.02"];
        private static readonly string[] CashPrefixes = ["50", "51"];
        private static readonly string[] DynamicsRoots = ["50", "51", "60", "62"];

        private readonly IDateTimePeriodService _dates;

        public DashboardService(
            IDatabaseContextFactory dbFactory,
            IDateTimePeriodService dates,
            ILogger<DashboardService> logger)
            : base(dbFactory, logger)
        {
            _dates = dates;
        }

        public Task<ServiceResult<DashboardKpiResponse>> GetKpiAsync(DateTime startDate, DateTime endDate)
        {
            return GetKpiInternalAsync(startDate, endDate);
        }

        public async Task<ServiceResult<DashboardResponse>> GetDashboardAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var periodError = ValidatePeriod(startDate, endDate, out var start, out var end);
            if (periodError is not null)
                return ServiceResult<DashboardResponse>.Fail(periodError);

            await using var db = CreateContext();

            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted)
                .ToListAsync();

            var accountMap = accounts.ToDictionary(a => a.id);
            var incomeIds = GetAccountIds(accounts, IncomePrefixes);
            var expenseIds = GetAccountIds(accounts, ExpensePrefixes);
            var cashIds = GetAccountIds(accounts, CashPrefixes);
            var dynamicsIds = GetDynamicsAccountIds(accounts);

            var lines = await LoadLineSnapsAsync(db, endUtc: end);

            var transactionsInPeriod = await db.Transactions
                .AsNoTracking()
                .CountAsync(t => !t.deleted && t.date >= start && t.date <= end);

            var kpi = BuildKpi(lines, incomeIds, expenseIds, cashIds, start, end, transactionsInPeriod);

            var groupByMonth = (end - start).TotalDays > 62;
            var incomeExpenseChart = BuildIncomeExpenseChart(lines, incomeIds, expenseIds, start, end, groupByMonth);
            var expenseStructure = BuildExpenseStructure(lines, expenseIds, accountMap, start, end);
            var balanceDynamics = BuildBalanceDynamics(lines, dynamicsIds, accountMap, start, end, groupByMonth);

            var recent = await BuildRecentTransactionsAsync(db, start, end);
            var topCounterparties = await BuildTopCounterpartiesAsync(db, lines, start, end);
            var alerts = await BuildAlertsAsync(db, lines, start, end);

            return ServiceResult<DashboardResponse>.Ok(new DashboardResponse
            {
                start_date = start,
                end_date = end,
                kpi = kpi,
                income_expense_chart = incomeExpenseChart,
                expense_structure = expenseStructure,
                balance_dynamics = balanceDynamics,
                recent_transactions = recent,
                top_counterparties = topCounterparties,
                alerts = alerts,
            });
        }

        private async Task<ServiceResult<DashboardKpiResponse>> GetKpiInternalAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var periodError = ValidatePeriod(startDate, endDate, out var start, out var end);
            if (periodError is not null)
                return ServiceResult<DashboardKpiResponse>.Fail(periodError);

            await using var db = CreateContext();

            var accounts = await db.Accounts
                .AsNoTracking()
                .Where(a => !a.deleted)
                .ToListAsync();

            var incomeIds = GetAccountIds(accounts, IncomePrefixes);
            var expenseIds = GetAccountIds(accounts, ExpensePrefixes);
            var cashIds = GetAccountIds(accounts, CashPrefixes);

            var lines = await LoadLineSnapsAsync(db, endUtc: end);

            var count = await db.Transactions
                .AsNoTracking()
                .CountAsync(t => !t.deleted && t.date >= start && t.date <= end);

            var kpi = BuildKpi(lines, incomeIds, expenseIds, cashIds, start, end, count);
            return ServiceResult<DashboardKpiResponse>.Ok(kpi);
        }

        private static async Task<List<LineSnap>> LoadLineSnapsAsync(
            ApplicationDbContext db,
            DateTime? startUtc = null,
            DateTime? endUtc = null)
        {
            var query = db.TransactionLines
                .AsNoTracking()
                .Where(l => !l.transaction.deleted);

            if (startUtc is not null)
                query = query.Where(l => l.transaction.date >= startUtc.Value);

            if (endUtc is not null)
                query = query.Where(l => l.transaction.date <= endUtc.Value);

            return await query
                .Select(l => new LineSnap(
                    l.id,
                    l.account_id,
                    l.side,
                    l.amount,
                    l.transaction.date,
                    l.transaction.id,
                    l.counterparty_id,
                    l.transaction.counterparty_id,
                    l.transaction.description,
                    l.transaction.is_complex,
                    l.transaction.debit_account_id,
                    l.transaction.credit_account_id))
                .ToListAsync();
        }

        private ServiceError? ValidatePeriod(
            DateTime startDate,
            DateTime endDate,
            out DateTime start,
            out DateTime end)
        {
            start = default;
            end = default;

            if (startDate == default || endDate == default)
                return ServiceErrors.Validation("Укажите параметры start_date и end_date.");

            start = _dates.ToUtcStart(startDate);
            end = _dates.ToUtcEnd(endDate);

            if (start > end)
                return ServiceErrors.Validation("Дата начала периода не может быть позже даты окончания.");

            return null;
        }

        private static DashboardKpiResponse BuildKpi(
            List<LineSnap> lines,
            HashSet<int> incomeIds,
            HashSet<int> expenseIds,
            HashSet<int> cashIds,
            DateTime start,
            DateTime end,
            int transactionsCount)
        {
            var periodLines = lines.Where(l => l.Date >= start && l.Date <= end).ToList();

            var income = SumTurnover(periodLines, incomeIds, EntrySide.credit)
                - SumTurnover(periodLines, incomeIds, EntrySide.debit);
            var expenses = SumTurnover(periodLines, expenseIds, EntrySide.debit)
                - SumTurnover(periodLines, expenseIds, EntrySide.credit);

            var cashBalance = CalcAccountBalance(lines, cashIds, end);

            return new DashboardKpiResponse
            {
                net_result = income - expenses,
                total_income = income,
                total_expenses = expenses,
                cash_balance = cashBalance,
                transactions_count = transactionsCount,
            };
        }

        private static List<IncomeExpenseChartPoint> BuildIncomeExpenseChart(
            List<LineSnap> lines,
            HashSet<int> incomeIds,
            HashSet<int> expenseIds,
            DateTime start,
            DateTime end,
            bool groupByMonth)
        {
            var periodLines = lines.Where(l => l.Date >= start && l.Date <= end).ToList();
            var keys = periodLines
                .Select(l => GetPeriodKey(l.Date, groupByMonth))
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            if (keys.Count == 0)
            {
                keys.Add(GetPeriodKey(start, groupByMonth));
            }

            return keys.Select(key =>
            {
                var bucket = periodLines.Where(l => GetPeriodKey(l.Date, groupByMonth) == key).ToList();
                var income = SumTurnover(bucket, incomeIds, EntrySide.credit)
                    - SumTurnover(bucket, incomeIds, EntrySide.debit);
                var expenses = SumTurnover(bucket, expenseIds, EntrySide.debit)
                    - SumTurnover(bucket, expenseIds, EntrySide.credit);
                return new IncomeExpenseChartPoint
                {
                    period = key,
                    income = income,
                    expenses = expenses,
                };
            }).ToList();
        }

        private static List<ExpenseStructureItem> BuildExpenseStructure(
            List<LineSnap> lines,
            HashSet<int> expenseIds,
            Dictionary<int, Account> accountMap,
            DateTime start,
            DateTime end)
        {
            return lines
                .Where(l => l.Date >= start && l.Date <= end && expenseIds.Contains(l.AccountId))
                .GroupBy(l => l.AccountId)
                .Select(g =>
                {
                    var debit = g.Where(x => x.Side == EntrySide.debit).Sum(x => x.Amount);
                    var credit = g.Where(x => x.Side == EntrySide.credit).Sum(x => x.Amount);
                    var amount = debit - credit;
                    accountMap.TryGetValue(g.Key, out var acc);
                    return new ExpenseStructureItem
                    {
                        account_id = g.Key,
                        number = acc?.number ?? "",
                        name = acc?.name ?? "",
                        amount = amount,
                    };
                })
                .Where(x => x.amount > 0)
                .OrderByDescending(x => x.amount)
                .Take(8)
                .ToList();
        }

        private static List<BalanceDynamicsPoint> BuildBalanceDynamics(
            List<LineSnap> lines,
            Dictionary<string, int> dynamicsAccounts,
            Dictionary<int, Account> accountMap,
            DateTime start,
            DateTime end,
            bool groupByMonth)
        {
            var result = new List<BalanceDynamicsPoint>();
            var periodKeys = new List<string>();
            var cursor = groupByMonth ? new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc) : start.Date;

            while (cursor <= end)
            {
                periodKeys.Add(GetPeriodKey(cursor, groupByMonth));
                cursor = groupByMonth ? cursor.AddMonths(1) : cursor.AddDays(1);
            }

            periodKeys = periodKeys.Distinct().ToList();

            foreach (var (rootNumber, accountId) in dynamicsAccounts)
            {
                accountMap.TryGetValue(accountId, out var acc);
                foreach (var key in periodKeys)
                {
                    var pointEnd = ParsePeriodEnd(key, groupByMonth, end);
                    var ids = accountMap.Values
                        .Where(a => a.number == rootNumber || a.number.StartsWith(rootNumber + "."))
                        .Select(a => a.id)
                        .ToHashSet();

                    var balance = CalcAccountBalance(lines, ids, pointEnd);
                    result.Add(new BalanceDynamicsPoint
                    {
                        period = key,
                        account_number = rootNumber,
                        account_name = acc?.name ?? rootNumber,
                        balance = balance,
                    });
                }
            }

            return result;
        }

        private async Task<List<DashboardRecentTransaction>> BuildRecentTransactionsAsync(
            ApplicationDbContext db,
            DateTime start,
            DateTime end)
        {
            var items = await db.Transactions
                .AsNoTracking()
                .Where(t => !t.deleted && t.date >= start && t.date <= end)
                .Include(t => t.debit_account)
                .Include(t => t.credit_account)
                .Include(t => t.lines)
                    .ThenInclude(l => l.account)
                .OrderByDescending(t => t.date)
                .ThenByDescending(t => t.id)
                .Take(10)
                .ToListAsync();

            return items.Select(t =>
            {
                string? debitNum;
                string? creditNum;

                if (t.is_complex)
                {
                    debitNum = string.Join(", ", t.lines
                        .Where(l => l.side == EntrySide.debit)
                        .Select(l => l.account.number)
                        .Distinct());
                    creditNum = string.Join(", ", t.lines
                        .Where(l => l.side == EntrySide.credit)
                        .Select(l => l.account.number)
                        .Distinct());
                }
                else
                {
                    debitNum = t.debit_account?.number;
                    creditNum = t.credit_account?.number;
                }

                var amount = t.is_complex
                    ? t.lines.Where(l => l.side == EntrySide.debit).Sum(l => l.amount)
                    : t.amount ?? 0;

                return new DashboardRecentTransaction
                {
                    id = t.id,
                    date = t.date,
                    debit_number = debitNum,
                    credit_number = creditNum,
                    amount = amount,
                    description = t.description,
                };
            }).ToList();
        }

        private static async Task<List<TopCounterpartyItem>> BuildTopCounterpartiesAsync(
            ApplicationDbContext db,
            List<LineSnap> lines,
            DateTime start,
            DateTime end)
        {
            var periodLines = lines.Where(l => l.Date >= start && l.Date <= end).ToList();

            var grouped = periodLines
                .Where(l => l.LineCounterpartyId is not null || l.TransactionCounterpartyId is not null)
                .GroupBy(l => l.LineCounterpartyId ?? l.TransactionCounterpartyId!.Value)
                .Select(g => new { Id = g.Key, Turnover = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Turnover)
                .Take(5)
                .ToList();

            if (grouped.Count == 0)
                return [];

            var ids = grouped.Select(x => x.Id).ToList();
            var names = await db.Counterparties
                .AsNoTracking()
                .Where(c => ids.Contains(c.id))
                .ToDictionaryAsync(c => c.id, c => c.name);

            return grouped.Select(g => new TopCounterpartyItem
            {
                id = g.Id,
                name = names.GetValueOrDefault(g.Id, $"Контрагент #{g.Id}"),
                turnover = g.Turnover,
                role_type = g.Turnover >= 0 ? "дебитор" : "кредитор",
            }).ToList();
        }

        private async Task<List<DashboardAlert>> BuildAlertsAsync(
            ApplicationDbContext db,
            List<LineSnap> lines,
            DateTime start,
            DateTime end)
        {
            var alerts = new List<DashboardAlert>();

            var withoutCounterparty = await db.Transactions
                .AsNoTracking()
                .CountAsync(t =>
                    !t.deleted
                    && t.date >= start
                    && t.date <= end
                    && t.counterparty_id == null
                    && !t.lines.Any(l => l.counterparty_id != null));

            if (withoutCounterparty > 0)
            {
                alerts.Add(new DashboardAlert
                {
                    type = "warning",
                    message = $"Найдено проводок без контрагента: {withoutCounterparty}",
                    link = "/transactions",
                });
            }

            var unbalanced = await db.Transactions
                .AsNoTracking()
                .Where(t => !t.deleted && t.date >= start && t.date <= end)
                .Include(t => t.lines)
                .ToListAsync();

            var unbalancedCount = unbalanced.Count(t =>
            {
                var debit = t.lines.Where(l => l.side == EntrySide.debit).Sum(l => l.amount);
                var credit = t.lines.Where(l => l.side == EntrySide.credit).Sum(l => l.amount);
                return Math.Abs(debit - credit) > 0.01m;
            });

            if (unbalancedCount > 0)
            {
                alerts.Add(new DashboardAlert
                {
                    type = "error",
                    message = $"Несбалансированные проводки за период: {unbalancedCount}",
                    link = "/transactions",
                });
            }

            var totalDebit = lines.Where(l => l.Date <= end).Where(l => l.Side == EntrySide.debit).Sum(l => l.Amount);
            var totalCredit = lines.Where(l => l.Date <= end).Where(l => l.Side == EntrySide.credit).Sum(l => l.Amount);
            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            {
                alerts.Add(new DashboardAlert
                {
                    type = "warning",
                    message = "Общий дебет и кредит по журналу не сходятся",
                    link = "/transactions",
                });
            }

            var now = DateTime.UtcNow;
            var reportingDeadline = new DateTime(now.Year, now.Month, 25, 0, 0, 0, DateTimeKind.Utc);
            if (now <= reportingDeadline)
            {
                alerts.Add(new DashboardAlert
                {
                    type = "info",
                    message = $"Срок сдачи отчётности: {reportingDeadline:dd.MM.yyyy}",
                });
            }

            return alerts;
        }

        private static decimal SumTurnover(
            IEnumerable<LineSnap> lines,
            HashSet<int> accountIds,
            EntrySide side) =>
            lines.Where(l => accountIds.Contains(l.AccountId) && l.Side == side).Sum(l => l.Amount);

        private static decimal CalcAccountBalance(List<LineSnap> lines, HashSet<int> accountIds, DateTime asOf)
        {
            var relevant = lines.Where(l => l.Date <= asOf && accountIds.Contains(l.AccountId)).ToList();
            var debit = relevant.Where(l => l.Side == EntrySide.debit).Sum(l => l.Amount);
            var credit = relevant.Where(l => l.Side == EntrySide.credit).Sum(l => l.Amount);
            return debit - credit;
        }

        private static HashSet<int> GetAccountIds(List<Account> accounts, string[] prefixes) =>
            accounts
                .Where(a => prefixes.Any(p => a.number == p || a.number.StartsWith(p + ".")))
                .Select(a => a.id)
                .ToHashSet();

        private static Dictionary<string, int> GetDynamicsAccountIds(List<Account> accounts) =>
            DynamicsRoots
                .Select(root => accounts.FirstOrDefault(a => a.number == root))
                .Where(a => a is not null)
                .ToDictionary(a => a!.number, a => a!.id);

        private static string GetPeriodKey(DateTime date, bool byMonth) =>
            byMonth ? date.ToString("yyyy-MM") : date.ToString("yyyy-MM-dd");

        private static DateTime ParsePeriodEnd(string key, bool byMonth, DateTime fallbackEnd)
        {
            if (byMonth)
            {
                var parts = key.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m))
                {
                    var lastDay = DateTime.DaysInMonth(y, m);
                    return new DateTime(y, m, lastDay, 23, 59, 59, DateTimeKind.Utc);
                }
            }
            else if (DateTime.TryParse(key, out var d))
            {
                return d.ToUniversalTime().Date.AddDays(1).AddTicks(-1);
            }

            return fallbackEnd;
        }

        private sealed record LineSnap(
            int Id,
            int AccountId,
            EntrySide Side,
            decimal Amount,
            DateTime Date,
            int TransactionId,
            int? LineCounterpartyId,
            int? TransactionCounterpartyId,
            string Description,
            bool IsComplex,
            int? DebitAccountId,
            int? CreditAccountId);
    }
}
