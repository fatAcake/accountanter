using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Infrastructure;

/// <summary>
/// Детерминированные данные для проверки ОСВ и двойной записи.
/// Пароль всех пользователей: <see cref="DatabaseSeed.DemoPassword"/>.
/// </summary>
public static class AccountingTestDataSeeder
{
    public const string IntegrationMarker = "__integration_test_seed__";

    public const string AdminEmail = "admin-test@integration.accountent.local";
    public const string AccountantEmail = "accountant-test@integration.accountent.local";
    public const string ObserverEmail = "observer-test@integration.accountent.local";

    public static readonly DateTime PeriodStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime PeriodEnd = new(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

    public static async Task SeedAsync(ApplicationDbContext db) =>
        await SeedAsync(db, forceReset: false);

    public static async Task EnsureFreshSeedAsync(ApplicationDbContext db) =>
        await SeedAsync(db, forceReset: true);

    private static async Task SeedAsync(ApplicationDbContext db, bool forceReset)
    {
        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
            return;

        var txCount = await db.Transactions.CountAsync(t => !t.deleted);
        if (!forceReset &&
            txCount == 3 &&
            await db.Transactions.AnyAsync(t => !t.deleted && t.description == IntegrationMarker))
            return;

        if (forceReset || txCount > 0)
            await ResetAccountingDataAsync(db);
        await ChartOfAccountsSeed.SeedAsync(db);

        var accounts = await db.Accounts.ToDictionaryAsync(a => a.number);
        var hash = BCrypt.Net.BCrypt.HashPassword(DatabaseSeed.DemoPassword);
        var now = DateTime.UtcNow;

        db.Users.AddRange(
            new Users
            {
                nickname = "Тест Админ",
                email = AdminEmail,
                password = hash,
                role = Roles.admin,
                registration_date = now,
            },
            new Users
            {
                nickname = "Тест Бухгалтер",
                email = AccountantEmail,
                password = hash,
                role = Roles.accountant,
                registration_date = now,
            },
            new Users
            {
                nickname = "Тест Наблюдатель",
                email = ObserverEmail,
                password = hash,
                role = Roles.observer,
                registration_date = now,
            });

        await db.SaveChangesAsync();

        var transactions = new List<Transaction>
        {
            MakeSimple(accounts, Day(2026, 1, 5), IntegrationMarker,
                "51.01", "62.01", 10_000m),
            MakeSimple(accounts, Day(2026, 1, 10), "Отражение выручки",
                "62.01", "90.01", 3_000m),
            MakeSimple(accounts, Day(2026, 1, 20), "Аванс на расчётный счёт",
                "51.01", "62.01", 5_000m),
        };

        db.Transactions.AddRange(transactions);
        await db.SaveChangesAsync();
    }

    public static OsvExpectedTotals ExpectedJanuary2026Osv() => new(
        Account51TurnoverDebit: 15_000m,
        Account51ClosingDebit: 15_000m,
        Account62TurnoverDebit: 3_000m,
        Account62TurnoverCredit: 15_000m,
        Account62ClosingCredit: 12_000m,
        Account90TurnoverCredit: 3_000m,
        Account90ClosingCredit: 3_000m,
        TotalTurnoverDebit: 18_000m,
        TotalTurnoverCredit: 18_000m,
        TotalClosingDebit: 15_000m,
        TotalClosingCredit: 15_000m);

    private static DateTime Day(int year, int month, int day) =>
        DateTime.SpecifyKind(new DateTime(year, month, day, 12, 0, 0), DateTimeKind.Utc);

    private static Transaction MakeSimple(
        Dictionary<string, Account> accounts,
        DateTime date,
        string description,
        string debitNumber,
        string creditNumber,
        decimal amount)
    {
        var debit = accounts[debitNumber];
        var credit = accounts[creditNumber];

        var tx = new Transaction
        {
            date = date,
            description = description,
            is_complex = false,
            debit_account_id = debit.id,
            credit_account_id = credit.id,
            amount = amount,
            created_at = date,
        };

        tx.lines.Add(new TransactionLine
        {
            account_id = debit.id,
            amount = amount,
            side = EntrySide.debit,
        });
        tx.lines.Add(new TransactionLine
        {
            account_id = credit.id,
            amount = amount,
            side = EntrySide.credit,
        });

        return tx;
    }

    private static async Task ResetAccountingDataAsync(ApplicationDbContext db)
    {
        db.TransactionLines.RemoveRange(await db.TransactionLines.ToListAsync());
        db.Transactions.RemoveRange(await db.Transactions.ToListAsync());
        db.Users.RemoveRange(await db.Users.ToListAsync());
        db.Counterparties.RemoveRange(await db.Counterparties.ToListAsync());
        await db.SaveChangesAsync();
    }

    public sealed record OsvExpectedTotals(
        decimal Account51TurnoverDebit,
        decimal Account51ClosingDebit,
        decimal Account62TurnoverDebit,
        decimal Account62TurnoverCredit,
        decimal Account62ClosingCredit,
        decimal Account90TurnoverCredit,
        decimal Account90ClosingCredit,
        decimal TotalTurnoverDebit,
        decimal TotalTurnoverCredit,
        decimal TotalClosingDebit,
        decimal TotalClosingCredit);
}
